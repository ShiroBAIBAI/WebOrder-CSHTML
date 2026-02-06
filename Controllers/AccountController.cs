using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;


namespace Demo.Controllers;

public class AccountController : Controller
{

//Clear guest cart
private void ClearGuestCart()
{
    try
    {
        HttpContext.Session.Remove("Cart");
        HttpContext.Session.Remove("CartCount");
        HttpContext.Session.Remove("GuestCart");
        HttpContext.Session.Remove("ShoppingCart");

        foreach (var key in Request.Cookies.Keys)
        {
            if (key.Contains("Cart"))
            {
                Response.Cookies.Delete(key);
            }
        }
    }
    catch { /* ignore */ }
}


    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration cfg;

    public AccountController(DB db, IWebHostEnvironment en, Helper hp,
                         IHttpClientFactory httpClientFactory,
                         IConfiguration cfg)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
        this.httpClientFactory = httpClientFactory;   
        this.cfg = cfg;
    }

    private bool VerifyCaptcha(string? token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var secret = cfg.GetSection("Recaptcha")["SecretKey"];
            if (string.IsNullOrWhiteSpace(secret)) return false;

            var client = httpClientFactory.CreateClient();
            var resp = client.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secret,
                    ["response"] = token,
                    ["remoteip"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
                })
            ).GetAwaiter().GetResult();

            if (!resp.IsSuccessStatusCode) return false;

            var json = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var ok) && ok.GetBoolean();
        }
        catch
        {
            return false;
        }
    }

    // GET: Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        int guestCartCount = 0;

        try
        {

            guestCartCount = HttpContext.Session.GetInt32("CartCount") ?? 0;
            guestCartCount = Math.Max(guestCartCount, HttpContext.Session.GetInt32("GuestCartCount") ?? 0);
            guestCartCount = Math.Max(guestCartCount, HttpContext.Session.GetInt32("ShoppingCartCount") ?? 0);
        }
        catch {}

        ViewBag.GuestCartCount = guestCartCount;
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        if (!ModelState.IsValid)
        {
            TempData["error"] = "Please correct the errors and try again.";
            return View(vm);
        }

        if (!VerifyCaptcha(vm.CaptchaToken))
        {
            ModelState.AddModelError("CaptchaToken", "Captcha verification failed. Please try again.");
            TempData["error"] = "Captcha verification failed.";
            return View(vm);
        }
        var u = db.Users.Find(vm.Email);
        if (u == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            TempData["error"] = "Invalid email or password.";
            return View(vm);
        }

        var nowUtc = DateTime.UtcNow;
        if (u.LockoutEnd.HasValue && u.LockoutEnd.Value > nowUtc)
        {
            var remain = u.LockoutEnd.Value - nowUtc;
            var mins = Math.Max(1, (int)Math.Ceiling(remain.TotalMinutes));
            ModelState.AddModelError("", $"Your account is locked. Please try again in {mins} minute(s).");
            TempData["error"] = $"Too many failed attempts. Locked for {mins} minute(s).";
            return View(vm);
        }

        var ok = hp.VerifyPassword(u.Hash, vm.Password);
        if (!ok)
        {
            u.AccessFailedCount = (u.AccessFailedCount + 1);

            if (u.AccessFailedCount >= 5)
            {
                u.LockoutEnd = nowUtc.AddMinutes(10);
                u.AccessFailedCount = 0; 
                db.SaveChanges();

                ModelState.AddModelError("", "Too many failed attempts. Your account is locked for 10 minutes.");
                TempData["error"] = "Too many failed attempts. Locked for 10 minutes.";
                return View(vm);
            }

            db.SaveChanges();
            var left = 5 - u.AccessFailedCount;
            ModelState.AddModelError("", $"Invalid email or password. {left} attempt(s) remaining before lock.");
            TempData["error"] = $"Invalid email or password. {left} attempt(s) left.";
            return View(vm);
        }

        u.AccessFailedCount = 0;
        u.LockoutEnd = null;
        db.SaveChanges();

        hp.SignIn(u.Email, u.Role, vm.RememberMe);
        ClearGuestCartAll();

        TempData["ok"] = "Login successfully.";
        if (!string.IsNullOrEmpty(returnURL)) return Redirect(returnURL);

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        hp.SignOut();
        TempData["ok"] = "Logout successfully.";
        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }

    // ------------------------------------------------------------------------
    // Register
    // ------------------------------------------------------------------------

    // GET: Account/Register
    public IActionResult Register()
    {
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        var sessEmail = HttpContext.Session.GetString("reg:email");
        var sessCode = HttpContext.Session.GetString("reg:code");
        var sessExp = HttpContext.Session.GetString("reg:expiry");

        if (ModelState.IsValid("Code"))
        {
            if (string.IsNullOrWhiteSpace(vm.Code) || string.IsNullOrWhiteSpace(sessCode))
                ModelState.AddModelError("Code", "Verification code is required.");
            else if (!string.Equals(vm.Email, sessEmail, StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError("Code", "Email not matched with verification code.");
            else if (!string.Equals(vm.Code.Trim(), sessCode, StringComparison.Ordinal))
                ModelState.AddModelError("Code", "Invalid verification code.");
            else if (DateTime.TryParse(sessExp, out var exp) && DateTime.UtcNow > exp)
                ModelState.AddModelError("Code", "Verification code expired.");
        }

        if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.IsValid("Photo") && vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            string photoName = vm.Photo != null
                ? hp.SavePhoto(vm.Photo, "photos")
                : "default.png";

            db.Members.Add(new()
            {
                Email = vm.Email,
                Hash = hp.HashPassword(vm.Password),
                Name = vm.Name,
                PhotoURL = photoName,
            });
            db.SaveChanges();

            HttpContext.Session.Remove("reg:email");
            HttpContext.Session.Remove("reg:code");
            HttpContext.Session.Remove("reg:expiry");

            TempData["ok"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        return View(vm);
    }

    [HttpPost]
    public IActionResult SendRegisterCode([FromForm] string email)
    {
        email = (email ?? "").Trim();
        if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
            return Json(new { ok = false, message = "Invalid email." });

        // generate  code
        var code = Random.Shared.Next(100000, 999999).ToString();

        HttpContext.Session.SetString("reg:email", email);
        HttpContext.Session.SetString("reg:code", code);
        HttpContext.Session.SetString("reg:expiry", DateTime.UtcNow.AddMinutes(10).ToString("O"));

        // send by verification email
        var body = $"<p>Your verification code is:</p><h2 style='letter-spacing:4px'>{code}</h2><p>It will expire in 10 minutes.</p>";
        var mail = new System.Net.Mail.MailMessage();
        mail.To.Add(new System.Net.Mail.MailAddress(email, email));
        mail.Subject = "Registration Verification Code";
        mail.IsBodyHtml = true;
        mail.Body = body;
        hp.SendEmail(mail);

        return Json(new { ok = true, message = "Verification code sent." });
    }


    // ------------------------------------------------------------------------
    // Update Password
    // ------------------------------------------------------------------------

    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        var u = db.Users.Find(User.Identity!.Name);
        if (u == null)
        {
            TempData["error"] = "User not found.";
            return RedirectToAction("Index", "Home");
        }

        if (string.IsNullOrWhiteSpace(vm.Current))
            ModelState.AddModelError(nameof(vm.Current), "Current password is required.");
        if (string.IsNullOrWhiteSpace(vm.New))
            ModelState.AddModelError(nameof(vm.New), "New password is required.");
        if (string.IsNullOrWhiteSpace(vm.Confirm))
            ModelState.AddModelError(nameof(vm.Confirm), "Confirm password is required.");

        if (!ModelState.IsValid)
        {
            TempData["error"] = "Failed to update password.";
            return View(vm);
        }

        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError(nameof(vm.Current), "Current password not matched.");
            TempData["error"] = "Failed to update password.";
            return View(vm);
        }

        u.Hash = hp.HashPassword(vm.New);
        db.SaveChanges();

        TempData["ok"] = "Password updated.";
        return RedirectToAction();
    }

    // ------------------------------------------------------------------------
    // Update Profile
    // ------------------------------------------------------------------------

    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        var m = db.Members.Find(User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            Name = m.Name,
            PhotoURL = m.PhotoURL,
        };

        return View(vm);
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Members.Find(User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            m.Name = vm.Name;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photos");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            db.SaveChanges();

            TempData["ok"] = "Profile updated.";
            return RedirectToAction();
        }

        vm.Email = m.Email;
        vm.PhotoURL = m.PhotoURL;
        return View(vm);
    }

    // ------------------------------------------------------------------------
    // Reset Password
    // ------------------------------------------------------------------------

    [AllowAnonymous]
    public IActionResult ResetPassword() => View();

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.Find(vm.Email);
        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
            TempData["error"] = "Email not found.";
            return View(vm);
        }
        var password = hp.RandomPassword();
        u.Hash = hp.HashPassword(password);
        db.SaveChanges();
        SendResetPasswordEmail(u, password);
        TempData["ok"] = "Password reset. Check your email.";
        return RedirectToAction(nameof(ResetPassword));
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        var url = Url.Action("Login", "Account", null, Request.Scheme); 

        string photoFile = "default.png";
        if (u is Member m && !string.IsNullOrWhiteSpace(m.PhotoURL)) photoFile = m.PhotoURL;

        var path = Path.Combine(en.WebRootPath ?? "wwwroot", "photos", photoFile);
        Attachment? att = null;
        try
        {
            if (System.IO.File.Exists(path))
            {
                att = new Attachment(path);
                mail.Attachments.Add(att);
                att.ContentId = "photo";
            }
        }
        catch {}

        var imgTag = (att != null) ? "<img src='cid:photo' style='width:200px;height:200px;border:1px solid #333'>" : "";
        mail.Body = $@"
        {imgTag}
        <p>Dear {u.Name},</p>
        <p>Your password has been reset to:</p>
        <h1 style='color:red'>{password}</h1>
        <p>Please <a href='{url}'>login</a> with your new password.</p>
        <p>From, Admin</p>";

        hp.SendEmail(mail);
    }

    private void ClearGuestCartAll()
    {
        try
        {
            HttpContext.Session.Clear();
            string[] commonKeys = { "Cart", "CartCount", "GuestCart", "ShoppingCart", "CartItems" };
            foreach (var k in commonKeys) HttpContext.Session.Remove(k);
        }
        catch { }

        try
        {
            var toDelete = new List<string>();
            foreach (var key in Request.Cookies.Keys)
            {
                var k = key ?? "";
                if (k.IndexOf("cart", StringComparison.OrdinalIgnoreCase) >= 0
                    || string.Equals(k, "CartId", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "cart-id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(k, "GuestCartId", StringComparison.OrdinalIgnoreCase))
                {
                    toDelete.Add(key);
                }
            }
            foreach (var k in toDelete)
            {
                Response.Cookies.Delete(k, new CookieOptions { Path = "/" });
            }
        }
        catch { }
    }

}
