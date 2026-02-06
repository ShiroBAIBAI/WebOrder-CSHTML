using Demo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;
    public class OrderController : Controller
    {
        private readonly DB db;
        private readonly Helper hp;
        private readonly EmailService _email;
        private readonly InvoiceService _invoice;
        private readonly PriceService _price;

        public OrderController(DB db, Helper hp, EmailService email, InvoiceService invoice, PriceService price)
        {
            this.db = db;
            this.hp = hp;
            _email = email;
            _invoice = invoice;
            _price = price;
        }

        public IActionResult Index() => View();

        public async Task<IActionResult> Catalog(string? cat)
        {
            IQueryable<MenuItem> q = db.MenuItems.AsQueryable();

            q = q.Include(m => m.Category)
                 .Include(m => m.Images);

            if (!string.IsNullOrWhiteSpace(cat) && int.TryParse(cat, out int catId))
                q = q.Where(m => m.MenuCategoryId == catId);

            var list = await q
                .OrderBy(m => m.Category.SortOrder)
                .ThenBy(m => m.SortOrder)
                .ToListAsync();

            return View("Catalog", list);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string menuItemId, int qty = 1, string? options = null)
        {
            var item = await db.MenuItems
                               .Include(m => m.Images)
                               .FirstOrDefaultAsync(m => m.MenuItemId == menuItemId);
            if (item == null) return NotFound();

            hp.AddToCart(item, qty, options);

            if (Request.Headers["Accept"].ToString().Contains("application/json"))
                return Json(new { ok = true, message = "Added to cart" });

            TempData["ok"] = "Added to cart";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public IActionResult UpdateCart(string menuItemId, int qty)
        {
            var cart = hp.GetCart();
            if (cart.TryGetValue(menuItemId, out var ci))
            {
                ci.Quantity = Math.Max(0, qty);
                if (ci.Quantity == 0) cart.Remove(menuItemId);
                hp.SetCart(cart);
            }
            TempData["ok"] = "Cart updated";
            return RedirectToAction(nameof(Cart));
        }

        [HttpPost]
        public IActionResult Remove(string menuItemId)
        {
            var cart = hp.GetCart();
            cart.Remove(menuItemId);
            hp.SetCart(cart);
            TempData["ok"] = "Item removed";
            return RedirectToAction(nameof(Cart));
        }

        public async Task<IActionResult> Cart()
        {
            var cart = hp.GetCart();                       
            var ids = cart.Keys.ToList();

            var dbItems = await db.MenuItems
                                  .Where(m => ids.Contains(m.MenuItemId))
                                  .AsNoTracking()
                                  .ToListAsync();

            var existingIds = new HashSet<string>(dbItems.Select(x => x.MenuItemId));
            var missingSet = new HashSet<string>(ids.Where(id => !existingIds.Contains(id)));

            var isActiveProp = typeof(MenuItem).GetProperty("IsActive");
            var availMap = dbItems.ToDictionary(
                x => x.MenuItemId,
                x => {
                    if (isActiveProp == null) return true;
                    var val = isActiveProp.GetValue(x);
                    return val is bool b ? b : true;
                });

            ViewBag.MissingSet = missingSet;   
            ViewBag.AvailMap = availMap;       
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(PaymentMethod payment, string? channel, string? idempotencyKey)
        {
            var cart = hp.GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                TempData["Info"] = "Order already processed or invalid submission.";
                return RedirectToAction(nameof(Summary));
            }
            idempotencyKey = idempotencyKey.Trim();

            var voucher = HttpContext.Session.GetString("voucher");
            var price = _price.Calculate(cart, voucher, null);

            //  Begin transaction for idempotent create 
            using var tx = await db.Database.BeginTransactionAsync();

            //  same key already exists, reuse 
            var existing = await db.Orders
                .Include(o => o.Lines).ThenInclude(l => l.MenuItem)
                .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);

            Order order;
            if (existing != null)
            {
                order = existing;
            }
            else
            {
                order = new Order
                {
                    MemberEmail = hp.LoginEmail(),
                    CreatedAt = DateTime.Now,
                    Subtotal = price.Subtotal,
                    Discount = price.Discount,
                    Tax = price.Tax,
                    Total = price.Total,
                    VoucherCode = price.VoucherCode,
                    PaymentMethod = payment.ToString(),
                    PaymentChannel = channel,
                    Status = "Placed",
                    IdempotencyKey = idempotencyKey
                };

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                foreach (var kv in cart)
                {
                    var ci = kv.Value;
                    db.OrderLines.Add(new OrderLine
                    {
                        OrderId = order.OrderId,
                        MenuItemId = ci.Item.MenuItemId,
                        Quantity = ci.Quantity,
                        Price = ci.Item.Price,
                        Options = ci.Options
                    });
                }
                await db.SaveChangesAsync();

                await tx.CommitAsync();
            }

            var orderFull = await db.Orders
                .Include(o => o.Lines).ThenInclude(l => l.MenuItem)
                .FirstAsync(o => o.OrderId == order.OrderId);

            byte[]? pdf = null;
            try { pdf = _invoice.Generate(orderFull, price); } catch { }
            try
            {
                var html = ReceiptTemplate.BuildHtml(orderFull, price);
                var to = string.IsNullOrWhiteSpace(orderFull.MemberEmail) ? hp.LoginEmail() : orderFull.MemberEmail;
                if (!string.IsNullOrWhiteSpace(to))
                    _email.SendEmail(to, $"E-Receipt #{orderFull.OrderId}", html, pdf, $"Receipt-{orderFull.OrderId}.pdf");
            }
            catch { }

            hp.ClearCart();

            if (User.Identity!.IsAuthenticated && User.IsInRole("Member"))
            {
                TempData["Info"] = $"Order #{order.OrderId} placed successfully.";
                return RedirectToAction("OrderDetails", "Order", new { id = order.OrderId });
            }

            if (payment == PaymentMethod.Online && (channel ?? "").Equals("stripe", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.AutoStripe = true;
                return View("PayOnline", orderFull);
            }

            return payment == PaymentMethod.Cashier
                ? View("PayAtCounter", orderFull)
                : View("PayOnline", orderFull);
        }

        public IActionResult Summary()
        {
            var cart = hp.GetCart();
            var voucher = HttpContext.Session.GetString("voucher");
            var price = _price.Calculate(cart, voucher, null);

            var vm = new OrderSummaryVM
            {
                Cart = cart,
                Price = price,
                VoucherCode = voucher,
                Payment = PaymentMethod.Cashier
            };
            ViewBag.IdempotencyKey = Guid.NewGuid().ToString("N");
            return View(vm);
        }

        [HttpGet, HttpPost]
        public IActionResult ApplyVoucher(string? voucherCode)
        {
            voucherCode = (voucherCode ?? "").Trim();
            if (string.IsNullOrEmpty(voucherCode))
                HttpContext.Session.Remove("voucher");
            else
                HttpContext.Session.SetString("voucher", voucherCode);

            return RedirectToAction(nameof(Summary));
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var email = hp.LoginEmail();
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login", "Account");

            var list = await db.Orders
                .Where(o => o.MemberEmail == email)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var email = hp.LoginEmail();
            var order = await db.Orders
                .Include(o => o.Lines).ThenInclude(l => l.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.MemberEmail == email);

            if (order == null) return NotFound();
            return View(order);
        }
    }

