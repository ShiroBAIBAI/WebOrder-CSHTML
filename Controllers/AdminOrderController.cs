using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly DB db;

        public AdminOrderController(DB db)
        {
            this.db = db;
        }

        // GET: AdminOrder
        public async Task<IActionResult> Index()
        {
            var orders = await db.Orders
                .Include(o => o.Lines).ThenInclude(l => l.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> CreateManualOrder()
        {
            var menu = await db.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Category.SortOrder)
                .ThenBy(m => m.SortOrder)
                .ToListAsync();

            return View(menu); 
        }

        //  POST: Create Manual Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManualOrder(string tableNo, string status, Dictionary<string, int> quantities)
        {
            if (quantities == null || !quantities.Any(q => q.Value > 0))
            {
                ModelState.AddModelError("", "Please select at least one menu item with quantity.");
                var menu = await db.MenuItems.Include(m => m.Category).ToListAsync();
                return View(menu);
            }

            var order = new Order
            {
                MemberEmail = null, 
                TableNo = tableNo,
                Status = string.IsNullOrEmpty(status) ? "Pending" : status,
                CreatedAt = DateTime.Now,
                Total = 0
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            foreach (var kv in quantities.Where(q => q.Value > 0))
            {
                var menuItem = await db.MenuItems.FindAsync(kv.Key);
                if (menuItem == null) continue;

                var line = new OrderLine
                {
                    OrderId = order.OrderId,
                    MenuItemId = menuItem.MenuItemId,
                    Quantity = kv.Value,
                    Price = menuItem.Price,
                    Options = null
                };

                order.Total += kv.Value * menuItem.Price;
                db.OrderLines.Add(line);
            }

            await db.SaveChangesAsync();

            TempData["ok"] = $"Manual order #{order.OrderId} created.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Update Order Status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await db.SaveChangesAsync();

            TempData["ok"] = $"Order #{id} status updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Update Table
        [HttpPost]
        public async Task<IActionResult> UpdateTable(int id, string tableNo)
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.TableNo = tableNo;
            await db.SaveChangesAsync();

            TempData["ok"] = $"Order #{id} assigned to table {tableNo}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Delete Order
        public async Task<IActionResult> Delete(int id)
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            db.Orders.Remove(order);
            await db.SaveChangesAsync();

            TempData["ok"] = $"Order #{id} deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
