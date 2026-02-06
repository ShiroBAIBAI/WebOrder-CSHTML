using System;
using System.Linq;
using Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;
    public class AdminController : Controller
    {
        private readonly DB _context;

        public AdminController(DB context)
        {
            _context = context;
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            return View(); 
        }

        //Settings
        [HttpGet]
        public IActionResult Settings()
        {
            ViewBag.CompanyName = _context.SystemSettings.FirstOrDefault(x => x.Key == "CompanyName")?.Value ?? "";
            ViewBag.CompanyAddress = _context.SystemSettings.FirstOrDefault(x => x.Key == "CompanyAddress")?.Value ?? "";
            ViewBag.CompanyPhone = _context.SystemSettings.FirstOrDefault(x => x.Key == "CompanyPhone")?.Value ?? "";
            ViewBag.TaxRate = _context.SystemSettings.FirstOrDefault(x => x.Key == "TaxRate")?.Value ?? "";

            var vouchers = _context.Vouchers
                .AsNoTracking()
                .OrderByDescending(v => v.Active)
                .ThenBy(v => v.Code)
                .ToList();

            return View(vouchers); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveTax(string taxRate, string? companyName, string? companyAddress, string? companyPhone)
        {
            if (string.IsNullOrWhiteSpace(taxRate)) taxRate = "0";

            // normalize accept 6/0.06
            if (decimal.TryParse(taxRate.Trim(), out var val))
            {
                if (val > 1m) val = val / 100m;
                UpsertSetting("TaxRate", val.ToString());
            }

            UpsertSetting("CompanyName", companyName);
            UpsertSetting("CompanyAddress", companyAddress);
            UpsertSetting("CompanyPhone", companyPhone);

            TempData["ok"] = "Settings saved.";
            return RedirectToAction(nameof(Settings));
        }

        private void UpsertSetting(string key, string? value)
        {
            var s = _context.SystemSettings.FirstOrDefault(x => x.Key == key);
            if (s == null)
                _context.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
            else
                s.Value = value;

            _context.SaveChanges();
        }

        // ===== Voucher CRUD =====

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateVoucher([Bind("Code,Type,Amount,MinSpend,ExpireAt")] Voucher v)
        {
            v.Code = (v.Code ?? "").Trim().ToUpperInvariant();
            v.Type = (v.Type ?? "percent").ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(v.Code))
                ModelState.AddModelError("Code", "Code is required.");
            if (v.Type != "percent" && v.Type != "fixed")
                ModelState.AddModelError("Type", "Type must be 'percent' or 'fixed'.");
            if (v.Amount <= 0)
                ModelState.AddModelError("Amount", "Amount must be greater than 0.");
            if (_context.Vouchers.Any(x => x.Code == v.Code))
                ModelState.AddModelError("Code", "Duplicated code.");

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Failed to create voucher.";
                return RedirectToAction(nameof(Settings));
            }

            v.Active = true; 
            _context.Vouchers.Add(v);
            _context.SaveChanges();

            TempData["ok"] = "Voucher created.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateVoucher([Bind("Code,Type,Amount,MinSpend,ExpireAt,Active")] Voucher v)
        {
            var key = (v.Code ?? "").Trim().ToUpperInvariant();
            var e = _context.Vouchers.Find(key);
            if (e == null)
            {
                TempData["error"] = "Voucher not found.";
                return RedirectToAction(nameof(Settings));
            }

            var type = (v.Type ?? "").ToLowerInvariant();
            if (type != "percent" && type != "fixed")
            {
                TempData["error"] = "Invalid type.";
                return RedirectToAction(nameof(Settings));
            }
            if (v.Amount <= 0)
            {
                TempData["error"] = "Amount must be greater than 0.";
                return RedirectToAction(nameof(Settings));
            }

            e.Type = type;
            e.Amount = v.Amount;
            e.MinSpend = v.MinSpend;
            e.ExpireAt = v.ExpireAt;
            e.Active = v.Active;

            _context.SaveChanges();
            TempData["ok"] = "Voucher updated.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteVoucher(string code)
        {
            var key = (code ?? "").Trim().ToUpperInvariant();
            var e = _context.Vouchers.Find(key);
            if (e == null)
            {
                TempData["error"] = "Voucher not found.";
                return RedirectToAction(nameof(Settings));
            }

            _context.Vouchers.Remove(e);
            _context.SaveChanges();

            TempData["ok"] = "Voucher deleted.";
            return RedirectToAction(nameof(Settings));
        }

        // Reports
        public IActionResult SalesReport()
        {
            //  MenuItem to safely access Name without relying on lazy-loading
            var salesData = _context.OrderLines
                .Include(l => l.MenuItem)
                .GroupBy(l => l.MenuItemId)
                .Select(g => new
                {
                    Product = g.First().MenuItem.Name,
                    Total = g.Sum(x => x.Quantity * x.MenuItem.Price),
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            ViewBag.Labels = salesData.Select(x => x.Product).ToArray();
            ViewBag.Data = salesData.Select(x => x.Total).ToArray();
            ViewBag.TotalSales = salesData.Sum(x => x.Total);
            ViewBag.MostPopular = salesData.FirstOrDefault()?.Product ?? "No sales yet";
            ViewBag.SalesData = salesData;

            return View();
        }
    }

