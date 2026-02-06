using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Demo.Controllers;
    [Authorize]
    public class StaffOrderController : Controller
    {
        private readonly DB db;
        private static readonly HashSet<string> KitchenCats =
            new(StringComparer.OrdinalIgnoreCase) { "Main Dish", "Snacks" };
        private static readonly HashSet<string> BarCats =
            new(StringComparer.OrdinalIgnoreCase) { "Drinks", "Dessert" };

        private static bool BelongsToRole(string categoryName, bool forBar)
        {
            var name = (categoryName ?? "").Trim();
            return forBar ? BarCats.Contains(name) : KitchenCats.Contains(name);
        }

        private static DateOnly? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var d))
                return d;
            return null;
        }

        public StaffOrderController(DB db) => this.db = db;

        [Authorize(Roles = "Kitchen")]
        public async Task<IActionResult> Kitchen(string q = "")
        {
            ViewData["Title"] = "Kitchen";
            ViewData["ForRole"] = "Kitchen";
            ViewData["IsHistory"] = false;
            var vm = await LoadQueueAsync(forBar: false, q, todayOnly: true, history: false, onDate: null);
            return View("Queue", vm);
        }

        [Authorize(Roles = "Bar")]
        public async Task<IActionResult> Bar(string q = "")
        {
            ViewData["Title"] = "Bar";
            ViewData["ForRole"] = "Bar";
            ViewData["IsHistory"] = false;
            var vm = await LoadQueueAsync(forBar: true, q, todayOnly: true, history: false, onDate: null);
            return View("Queue", vm);
        }

        [Authorize(Roles = "Kitchen")]
        public async Task<IActionResult> HistoryKitchen(string? date = null, string q = "")
        {
            ViewData["Title"] = "Kitchen History";
            ViewData["ForRole"] = "Kitchen";
            ViewData["IsHistory"] = true;
            var vm = await LoadQueueAsync(forBar: false, q, todayOnly: false, history: true, onDate: ParseDate(date));
            return View("Queue", vm);
        }

        [Authorize(Roles = "Bar")]
        public async Task<IActionResult> HistoryBar(string? date = null, string q = "")
        {
            ViewData["Title"] = "Bar History";
            ViewData["ForRole"] = "Bar";
            ViewData["IsHistory"] = true;
            var vm = await LoadQueueAsync(forBar: true, q, todayOnly: false, history: true, onDate: ParseDate(date));
            return View("Queue", vm);
        }

        // AJAX Refresh
        [HttpGet]
        [Authorize(Roles = "Kitchen,Bar")]
        public async Task<IActionResult> List(
            string role, string q = "", bool todayOnly = true, bool history = false,
            string? date = null, string view = null)  
        {
            bool forBar = string.Equals(role, "Bar", StringComparison.OrdinalIgnoreCase);
            var useView = view ?? (history ? "All" : "Active");
            var vm = await LoadQueueAsync(forBar, q, todayOnly, history, ParseDate(date), useView);
            return PartialView("_QueueList", vm);
        }
        private async Task<List<StaffOrderVM>> LoadQueueAsync(
    bool forBar, string q, bool todayOnly, bool history, DateOnly? onDate, string view = "Active")
        {
            var baseOrderQ = db.Orders.AsNoTracking().AsQueryable();

            if (todayOnly)
                baseOrderQ = baseOrderQ.Where(o => o.CreatedAt.Date == DateTime.Today);
            else if (onDate.HasValue)
                baseOrderQ = baseOrderQ.Where(o => DateOnly.FromDateTime(o.CreatedAt) == onDate.Value);

            HashSet<string> allowed;
            if (!history)
            {
                if (string.Equals(view, "Placed", StringComparison.OrdinalIgnoreCase))
                    allowed = new(new[] { "Placed" }, StringComparer.OrdinalIgnoreCase);
                else if (string.Equals(view, "InProgress", StringComparison.OrdinalIgnoreCase))
                    allowed = new(new[] { "InProgress" }, StringComparer.OrdinalIgnoreCase);
                else if (string.Equals(view, "All", StringComparison.OrdinalIgnoreCase))
                    allowed = new(new[] { "Placed", "InProgress" }, StringComparer.OrdinalIgnoreCase);
                else // Active
                    allowed = new(new[] { "Placed", "InProgress" }, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // All/Completed/Cancelled
                if (string.Equals(view, "Completed", StringComparison.OrdinalIgnoreCase))
                    allowed = new(new[] { "Completed" }, StringComparer.OrdinalIgnoreCase);
                else if (string.Equals(view, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    allowed = new(new[] { "Cancelled" }, StringComparer.OrdinalIgnoreCase);
                else // All
                    allowed = new(new[] { "Completed", "Cancelled" }, StringComparer.OrdinalIgnoreCase);
            }

            var orderWithLines = await baseOrderQ
                .Select(o => new
                {
                    o.OrderId,
                    o.TableNo,
                    o.CreatedAt,
                    o.Status,
                    Lines = db.OrderLines.Where(l => l.OrderId == o.OrderId).ToList()
                })
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            var menuItemIds = orderWithLines.SelectMany(x => x.Lines.Select(l => l.MenuItemId)).Distinct().ToList();
            var menuItems = await db.MenuItems
                .Where(mi => menuItemIds.Contains(mi.MenuItemId))
                .Select(mi => new { mi.MenuItemId, mi.MenuCategoryId, mi.Name })
                .ToDictionaryAsync(x => x.MenuItemId, x => x);
            var catIds = menuItems.Values.Select(x => x.MenuCategoryId).Distinct().ToList();
            var cats = await db.MenuCategories
                .Where(c => catIds.Contains(c.MenuCategoryId))
                .Select(c => new { c.MenuCategoryId, c.Name })
                .ToDictionaryAsync(x => x.MenuCategoryId, x => x.Name);

            var result = new List<StaffOrderVM>();

            foreach (var o in orderWithLines)
            {
                var lines = new List<StaffOrderLineVM>();

                foreach (var l in o.Lines)
                {
                    if (!menuItems.TryGetValue(l.MenuItemId, out var mi)) continue;
                    var catName = cats.TryGetValue(mi.MenuCategoryId, out var cn) ? (cn ?? "") : "";
                    if (!BelongsToRole(catName, forBar)) continue;

                    if (!allowed.Contains(l.LineStatus)) continue;

                    lines.Add(new StaffOrderLineVM
                    {
                        OrderLineId = l.OrderLineId,
                        MenuItemId = l.MenuItemId,
                        ItemName = mi.Name,
                        Quantity = l.Quantity,
                        Options = l.Options ?? string.Empty,
                        LineStatus = l.LineStatus
                    });
                }

                if (lines.Any())
                {
                    result.Add(new StaffOrderVM
                    {
                        OrderId = o.OrderId,
                        TableNo = o.TableNo,
                        CreatedAt = o.CreatedAt,
                        Status = o.Status,
                        Lines = lines
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToLowerInvariant();
                result = result.Where(or =>
                        (or.TableNo ?? "").ToLowerInvariant().Contains(key) ||
                        or.Lines.Any(l => (l.ItemName ?? "").ToLowerInvariant().Contains(key))
                    ).ToList();
            }

            return result.OrderBy(vm => vm.CreatedAt).ToList();
        }

        //Auto take order - Placed  -> InProgress
        [HttpPost]
        [Authorize(Roles = "Kitchen,Bar")]
        public async Task<IActionResult> AcceptVisible([FromBody] int[] lineIds)
        {
            if (lineIds == null || lineIds.Length == 0) return Ok(new { ok = true, n = 0 });

            var lines = await db.OrderLines
                .Where(l => lineIds.Contains(l.OrderLineId))
                .ToListAsync(); 

            int n = 0;
            foreach (var l in lines)
            {
                var order = await db.Orders.FindAsync(l.OrderId);
                if (order == null || order.CreatedAt.Date != DateTime.Today) continue;

                if (string.Equals(l.LineStatus, "Placed", StringComparison.OrdinalIgnoreCase))
                {
                    l.LineStatus = "InProgress";
                    n++;
                }
            }
            if (n > 0) await db.SaveChangesAsync();
            return Ok(new { ok = true, n });
        }

        [HttpPost]
        [Authorize(Roles = "Kitchen,Bar")]
        public async Task<IActionResult> UpdateLineStatus(int lineId, string to)
        {
            var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "InProgress","Completed","Cancelled" };
            if (!allow.Contains(to)) return BadRequest("Invalid status");

            var line = await db.OrderLines.FindAsync(lineId);
            if (line == null) return NotFound();

            var order = await db.Orders.FindAsync(line.OrderId);
            if (order == null || order.CreatedAt.Date != DateTime.Today)
                return BadRequest("Only today's orders can be modified.");

            var mi = await db.MenuItems.AsNoTracking()
                .Where(x => x.MenuItemId == line.MenuItemId)
                .Select(x => new { x.MenuCategoryId })
                .FirstOrDefaultAsync();
            if (mi != null)
            {
                var catName = await db.MenuCategories
                    .Where(c => c.MenuCategoryId == mi.MenuCategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync() ?? "";

                var forBar = User.IsInRole("Bar");
                if (!BelongsToRole(catName, forBar))
                    return Forbid(); 
            }

            line.LineStatus = to;
            await db.SaveChangesAsync();
            return Ok(new { ok = true });

        }

        [HttpPost]
        [Authorize(Roles = "Kitchen,Bar")]
        public async Task<IActionResult> UpdateLinesStatus([FromBody] UpdateManyReq req)
        {
            if (req == null || req.LineIds == null || req.LineIds.Length == 0)
                return BadRequest("No lines.");

            var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "InProgress","Completed","Cancelled" };
            if (!allow.Contains(req.To)) return BadRequest("Invalid status");

           bool forBar = User.IsInRole("Bar");
            var lines = await db.OrderLines
                .Where(l => req.LineIds.Contains(l.OrderLineId))
                .ToListAsync();

            if (lines.Count == 0) return Ok(new { ok = true, n = 0 });

            var itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();
            var items = await db.MenuItems
                .Where(mi => itemIds.Contains(mi.MenuItemId))
                .Select(mi => new { mi.MenuItemId, mi.MenuCategoryId })
                .ToDictionaryAsync(x => x.MenuItemId, x => x);

            var catIds = items.Values.Select(x => x.MenuCategoryId).Distinct().ToList();
            var cats = await db.MenuCategories
                .Where(c => catIds.Contains(c.MenuCategoryId))
                .Select(c => new { c.MenuCategoryId, c.Name })
                .ToDictionaryAsync(x => x.MenuCategoryId, x => x.Name);

            int n = 0;
            foreach (var l in lines)
            {
                var order = await db.Orders.FindAsync(l.OrderId);
                if (order == null || order.CreatedAt.Date != DateTime.Today)
                    continue;

                // Completed/Cancelled no changed
                if (string.Equals(l.LineStatus, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(l.LineStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!items.TryGetValue(l.MenuItemId, out var mi)) continue;
                var catName = cats.TryGetValue(mi.MenuCategoryId, out var cn) ? (cn ?? "") : "";
                bool belongsToBar = BarCats.Contains(catName);
                bool belongsToKitchen = KitchenCats.Contains(catName);
                if (!BelongsToRole(catName, forBar)) continue;

                l.LineStatus = req.To;
                n++;
            }
            if (n > 0) await db.SaveChangesAsync();
            return Ok(new { ok = true, n });
        }

        public class UpdateManyReq
        {
            public int[] LineIds { get; set; } = Array.Empty<int>();
            public string To { get; set; } = "InProgress";
        }
 
    }

    // ===== ViewModels =====
    public class StaffOrderVM
    {
        public int OrderId { get; set; }
        public string TableNo { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "";
        public List<StaffOrderLineVM> Lines { get; set; } = new();
        public int TotalItems => Lines.Count;
        public int TotalQty => Lines.Sum(x => x.Quantity);
        public TimeSpan Elapsed => DateTime.Now - CreatedAt;
    }

    public class StaffOrderLineVM
    {
        public int OrderLineId { get; set; }
        public string MenuItemId { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public string Options { get; set; } = "";
        public string LineStatus { get; set; } = "Placed";
    }


