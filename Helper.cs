using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Demo
{
    public class Helper
    {
        private readonly IWebHostEnvironment en;
        private readonly IHttpContextAccessor ct;
        private readonly IConfiguration cf;

        private static readonly JsonSerializerOptions CartJsonOpt = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = null,
            WriteIndented = false
        };

        public Helper(IWebHostEnvironment en,
                      IHttpContextAccessor ct,
                      IConfiguration cf)
        {
            this.en = en;
            this.ct = ct;
            this.cf = cf;
        }

        // Avatar / Gravatar
        public string GravatarUrl(string? email, int size = 32)
        {
            if (string.IsNullOrWhiteSpace(email))
                return $"https://www.gravatar.com/avatar/?s={size}&d=identicon";

            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return $"https://www.gravatar.com/avatar/{sb}?s={size}&d=identicon";
        }

        public string AvatarUrlOrGravatar(string? explicitUrl, string? email, int size = 32)
            => !string.IsNullOrWhiteSpace(explicitUrl) ? explicitUrl! : GravatarUrl(email, size);

        // Photo Upload
        public string ValidatePhoto(IFormFile f)
        {
            var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
            var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

            if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
            {
                return "Only JPG and PNG photo is allowed.";
            }
            else if (f.Length > 1 * 1024 * 1024)
            {
                return "Photo size cannot more than 1MB.";
            }

            return "";
        }

        public string SavePhoto(IFormFile f, string folder)
        {
            if (f == null || f.Length == 0)
            {
                return "default.png"; 
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(f.FileName);
            string savePath = Path.Combine(en.WebRootPath, folder, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                f.CopyTo(stream);
            }

            return fileName;
        }

        public void DeletePhoto(string file, string folder)
        {
            file = Path.GetFileName(file);
            var path = Path.Combine(en.WebRootPath, folder, file);
            File.Delete(path);
        }

        // Security / Auth
        private readonly PasswordHasher<object> ph = new();

        public string HashPassword(string password) => ph.HashPassword(0, password);

        public bool VerifyPassword(string hash, string password) =>
            ph.VerifyHashedPassword(0, hash, password) == PasswordVerificationResult.Success;

        public void SignIn(string email, string role, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = rememberMe
            };

            ct.HttpContext!.SignInAsync(principal, properties).GetAwaiter().GetResult();
        }

        public void SignOut()
        {
            ct.HttpContext!.SignOutAsync().GetAwaiter().GetResult();
        }

        public string RandomPassword()
        {
            string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string password = "";
            Random r = new();

            for (int i = 1; i <= 10; i++)
            {
                password += s[r.Next(s.Length)];
            }

            return password;
        }

        public string LoginEmail() =>
            ct.HttpContext?.User?.Identity?.IsAuthenticated == true
                ? ct.HttpContext!.User.Identity!.Name ?? string.Empty
                : string.Empty;

        private bool IsMember()
            => ct.HttpContext?.User?.Identity?.IsAuthenticated == true
               && ct.HttpContext!.User.IsInRole("Member");

        // Email
        public void SendEmail(MailMessage mail)
        {
            string user = cf["Smtp:User"] ?? "";
            string pass = cf["Smtp:Pass"] ?? "";
            string name = cf["Smtp:Name"] ?? "";
            string host = cf["Smtp:Host"] ?? "";
            int port = cf.GetValue<int>("Smtp:Port");

            mail.From = new MailAddress(user, name);

            using var smtp = new SmtpClient
            {
                Host = host,
                Port = port,
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass),
            };

            smtp.Send(mail);
        }

        // DateTime Helpers
        public SelectList GetMonthList()
        {
            var list = new List<object>();
            for (int n = 1; n <= 12; n++)
            {
                list.Add(new
                {
                    Id = n,
                    Name = new DateTime(1, n, 1).ToString("MMMM"),
                });
            }
            return new SelectList(list, "Id", "Name");
        }

        public SelectList GetYearList(int min, int max, bool reverse = false)
        {
            var list = new List<int>();
            for (int n = min; n <= max; n++) list.Add(n);
            if (reverse) list.Reverse();
            return new SelectList(list);
        }

        // Shopping Cart 
        public Dictionary<string, CartItem> GetCart()
        {
            if (!IsMember())
            {
                var s = ct.HttpContext!.Session.GetString("cart");
                if (string.IsNullOrEmpty(s)) return new();
                return JsonSerializer.Deserialize<Dictionary<string, CartItem>>(s, CartJsonOpt) ?? new();
            }
            else
            {
                var email = LoginEmail();
                using var scope = ct.HttpContext!.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DB>();

                var order = db.Orders
                    .Include(o => o.Lines).ThenInclude(l => l.MenuItem)
                    .FirstOrDefault(o => o.MemberEmail == email && o.Status == "Pending");

                if (order == null) return new();

                return order.Lines.ToDictionary(
                    l => l.MenuItemId,
                    l => new CartItem
                    {
                        Item = l.MenuItem,
                        Quantity = l.Quantity,
                        Options = l.Options
                    });
            }
        }

        public void SetCart(Dictionary<string, CartItem> cart)
        {
            if (!IsMember())
            {
                var s = JsonSerializer.Serialize(cart, CartJsonOpt);
                ct.HttpContext!.Session.SetString("cart", s);
            }
            else
            {
                var email = LoginEmail();
                using var scope = ct.HttpContext!.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DB>();

                var order = db.Orders
                    .Include(o => o.Lines)
                    .FirstOrDefault(o => o.MemberEmail == email && o.Status == "Pending");

                if (order == null)
                {
                    order = new Order
                    {
                        MemberEmail = email,
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };
                    db.Orders.Add(order);
                    db.SaveChanges();
                }

                // clear existing lines
                var existing = db.OrderLines.Where(l => l.OrderId == order.OrderId).ToList();
                if (existing.Any())
                {
                    db.OrderLines.RemoveRange(existing);
                    db.SaveChanges();
                }

                foreach (var kv in cart)
                {
                    db.OrderLines.Add(new OrderLine
                    {
                        OrderId = order.OrderId,
                        MenuItemId = kv.Value.Item.MenuItemId,
                        Quantity = kv.Value.Quantity,
                        Price = kv.Value.Item.Price,
                        Options = kv.Value.Options
                    });
                }

                db.SaveChanges();
            }
        }


        public void AddToCart(MenuItem item, int qty, string? options)
        {
            var cart = GetCart();

            if (!cart.TryGetValue(item.MenuItemId, out var ci))
            {
                var slim = new MenuItem
                {
                    MenuItemId = item.MenuItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Description = item.Description
                };

                ci = new CartItem { Item = slim, Quantity = 0, Options = options };
                cart[item.MenuItemId] = ci;
            }

            ci.Quantity += Math.Max(1, qty);
            if (!string.IsNullOrWhiteSpace(options)) ci.Options = options;

            SetCart(cart);
        }

        public void ClearCart()
        {
            if (!IsMember())
            {
                ct.HttpContext!.Session.Remove("cart");
            }
            else
            {
                var email = LoginEmail();
                using var scope = ct.HttpContext!.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DB>();

                var order = db.Orders.FirstOrDefault(o => o.MemberEmail == email && o.Status == "Pending");
                if (order != null)
                {
                    var lines = db.OrderLines.Where(l => l.OrderId == order.OrderId).ToList();
                    if (lines.Any()) db.OrderLines.RemoveRange(lines);
                    db.Orders.Remove(order);
                    db.SaveChanges();
                }
            }
        }
    }
}


