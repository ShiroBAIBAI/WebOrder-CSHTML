using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly DB db;
        private readonly Helper hp;

        public InventoryController(DB db, Helper hp)
        {
            this.db = db;
            this.hp = hp;
        }

        // Index + AJAX List
        public async Task<IActionResult> Index()
        {
            var query = db.StockItems.AsNoTracking().OrderBy(s => s.Name);
            var list = await query.ToListAsync();

            var ids = list.Select(x => x.StockItemId).ToList();
            var restocked = await db.InventoryTxns.AsNoTracking()
                .Where(t => ids.Contains(t.StockItemId) && t.QtyChange > 0)
                .GroupBy(t => t.StockItemId)
                .Select(g => new { g.Key, Last = g.Max(x => x.CreatedAt) })
                .ToDictionaryAsync(x => x.Key, x => (DateTime?)x.Last);

            ViewBag.RestockedAt = restocked;
            return View(list); 
        }

        [HttpGet]
        public async Task<IActionResult> List(string? q = "", string sort = "name", string dir = "asc")
        {
            var query = db.StockItems.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(s => s.Name.Contains(q) || (s.Category ?? "").Contains(q) || (s.Unit ?? "").Contains(q));

            query = (sort, dir) switch
            {
                ("qty", "asc") => query.OrderBy(s => s.Quantity),
                ("qty", "desc") => query.OrderByDescending(s => s.Quantity),
                ("name", "desc") => query.OrderByDescending(s => s.Name),
                _ => query.OrderBy(s => s.Name)
            };

            var list = await query.ToListAsync();

            var ids = list.Select(x => x.StockItemId).ToList();
            var restocked = await db.InventoryTxns.AsNoTracking()
                .Where(t => ids.Contains(t.StockItemId) && t.QtyChange > 0)
                .GroupBy(t => t.StockItemId)
                .Select(g => new { g.Key, Last = g.Max(x => x.CreatedAt) })
                .ToDictionaryAsync(x => x.Key, x => (DateTime?)x.Last);

            ViewBag.RestockedAt = restocked; 

            return PartialView("_List", list);
        }

        public IActionResult Create() => View(new StockItem());

        private static readonly string[] CATEGORY_WHITELIST = new[]
        { "Produce", "Meat", "Seafood", "Packaging", "Cleaning" };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockItem m)
        {
            if (string.IsNullOrWhiteSpace(m.Name))
                ModelState.AddModelError(nameof(m.Name), "Name is required.");
            if (string.IsNullOrWhiteSpace(m.Category) || !CATEGORY_WHITELIST.Contains(m.Category))
                ModelState.AddModelError(nameof(m.Category), "Category is required and must be valid.");
            if (m.Quantity < 0) ModelState.AddModelError(nameof(m.Quantity), "Quantity must be >= 0.");
            if (m.ReorderLevel < 0) ModelState.AddModelError(nameof(m.ReorderLevel), "Reorder must be >= 0.");
            if (await db.StockItems.AnyAsync(s => s.Name == m.Name))
                ModelState.AddModelError(nameof(m.Name), "Name already exists.");
            if (!ModelState.IsValid) return View(m);

            m.StockItemId = "IT" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            m.IsActive = m.Quantity > 0;

            db.StockItems.Add(m);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StockItem m)
        {
            var entity = await db.StockItems.FirstOrDefaultAsync(s => s.StockItemId == m.StockItemId);
            if (entity == null) return NotFound();


            if (string.IsNullOrWhiteSpace(m.Name))
                ModelState.AddModelError(nameof(m.Name), "Name is required.");
            if (string.IsNullOrWhiteSpace(m.Category) || !CATEGORY_WHITELIST.Contains(m.Category))
                ModelState.AddModelError(nameof(m.Category), "Category is required and must be valid.");
            if (m.ReorderLevel < 0)
                ModelState.AddModelError(nameof(m.ReorderLevel), "Reorder must be >= 0.");

            if (await db.StockItems.AnyAsync(s => s.Name == m.Name && s.StockItemId != m.StockItemId))
                ModelState.AddModelError(nameof(m.Name), "Name already exists.");

            if (!ModelState.IsValid) return View(entity);


            entity.Name = m.Name;
            entity.Unit = m.Unit;
            entity.ReorderLevel = m.ReorderLevel;
            entity.Category = m.Category;

            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(string id, string mode, int qty, string? remark)
        {
            var item = await db.StockItems.FirstOrDefaultAsync(s => s.StockItemId == id);
            if (item == null) return NotFound();

            var m = (mode ?? "").Trim().ToLowerInvariant();

            if (qty < 0)
            {
                TempData["Err"] = "Quantity must be >= 0.";
                return RedirectToAction(nameof(Adjust), new { id });
            }
            if (m is not ("add" or "sub" or "set"))
            {
                TempData["Err"] = "Invalid mode.";
                return RedirectToAction(nameof(Adjust), new { id });
            }

            int delta = m switch
            {
                "add" => qty,
                "sub" => -qty,
                "set" => qty - item.Quantity,
                _ => 0
            };

            if (item.Quantity + delta < 0)
            {
                TempData["Err"] = $"Not enough stock. Current: {item.Quantity}, requested: {qty}.";
                return RedirectToAction(nameof(Adjust), new { id });
            }

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                item.Quantity += delta;

                if (item.Quantity == 0) item.IsActive = false;

                db.InventoryTxns.Add(new InventoryTxn
                {
                    StockItemId = item.StockItemId,
                    QtyChange = delta,
                    TxnType = m.ToUpperInvariant(),
                    Remark = remark,
                    CreatedAt = DateTime.Now,
                    CreatedBy = hp.LoginEmail()
                });

                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }

            TempData["Ok"] = "Stock adjusted.";
            return RedirectToAction(nameof(Index));
        }

        //  GET: /Inventory/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var m = await db.StockItems.FindAsync(id);
            if (m == null) return NotFound();
            return View(m);
        }

        //  GET: /Inventory/Adjust/{id}
        public async Task<IActionResult> Adjust(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var m = await db.StockItems.FindAsync(id);
            if (m == null) return NotFound();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStock(string id, string mode)
        {
            var item = await db.StockItems.FirstOrDefaultAsync(s => s.StockItemId == id);
            if (item == null) return NotFound();

            if (string.Equals(mode, "on", StringComparison.OrdinalIgnoreCase) && item.Quantity == 0)
            {
                TempData["Err"] = "Cannot set On Stock when quantity is 0.";
                return RedirectToAction(nameof(Index));
            }

            item.IsActive = !string.Equals(mode, "out", StringComparison.OrdinalIgnoreCase);
            await db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkOut()
        {
            await db.StockItems.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkOn()
        {
            await db.StockItems.Where(x => x.Quantity > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var item = await db.StockItems
                .Include(s => s.Txns)
                .FirstOrDefaultAsync(s => s.StockItemId == id);
            if (item == null) return NotFound();

            if (item.Txns != null && item.Txns.Any())
            {
                TempData["Err"] = $"Cannot delete {item.Name}: it has inventory transactions.";
                return RedirectToAction(nameof(Index));
            }

            db.StockItems.Remove(item);
            await db.SaveChangesAsync();
            TempData["Ok"] = $"Deleted {item.Name}.";
            return RedirectToAction(nameof(Index));
        }


    }

