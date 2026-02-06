using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using System.Globalization;

namespace Demo.Controllers;
    [Authorize(Roles = "Admin")]
    public class MenuItemsController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment env;

        public MenuItemsController(DB db, IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
        }

        public async Task<IActionResult> Index(string? cat, string? q, string sort = "order", string dir = "asc")
        {
            var query = db.MenuItems
                          .Include(m => m.Category)
                          .Include(m => m.Images)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(cat) && int.TryParse(cat, out int catId))
                query = query.Where(m => m.MenuCategoryId == catId);

            if (!string.IsNullOrEmpty(q))
                query = query.Where(m => m.Name.Contains(q) || (m.Description ?? "").Contains(q));

            query = sort switch
            {
                "name" => dir == "asc" ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name),
                "price" => dir == "asc" ? query.OrderBy(m => m.Price) : query.OrderByDescending(m => m.Price),
                "category" => dir == "asc" ? query.OrderBy(m => m.Category.Name) : query.OrderByDescending(m => m.Category.Name),
                _ => dir == "asc" ? query.OrderBy(m => m.SortOrder) : query.OrderByDescending(m => m.SortOrder),
            };

            ViewBag.Categories = new SelectList(await db.MenuCategories.ToListAsync(), "MenuCategoryId", "Name");
            ViewBag.CurrentCat = cat;
            ViewBag.CurrentQ = q;
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentDir = dir;

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var item = await db.MenuItems
                .Include(m => m.Category)
                .Include(m => m.Images)
                .FirstOrDefaultAsync(m => m.MenuItemId == id);

            if (item == null) return NotFound();

            return View(item);
        }

        public IActionResult Create()
        {
            ViewData["MenuCategoryId"] = new SelectList(db.MenuCategories, "MenuCategoryId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItem menuItem, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {

                menuItem.MenuItemId = "MI" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

                db.Add(menuItem);
                await db.SaveChangesAsync();

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploads = Path.Combine(env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    var img = new MenuItemImage
                    {
                        ImageId = "IM" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                        MenuItemId = menuItem.MenuItemId,
                        Url = "/uploads/" + fileName,
                        SortOrder = 1
                    };
                    db.MenuItemImages.Add(img);
                    await db.SaveChangesAsync();
                }

                TempData["ok"] = "Menu item created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MenuCategoryId"] = new SelectList(db.MenuCategories, "MenuCategoryId", "Name", menuItem.MenuCategoryId);
            return View(menuItem);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var item = await db.MenuItems.Include(m => m.Images).FirstOrDefaultAsync(m => m.MenuItemId == id);
            if (item == null) return NotFound();

            ViewData["MenuCategoryId"] = new SelectList(db.MenuCategories, "MenuCategoryId", "Name", item.MenuCategoryId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, MenuItem menuItem, IFormFile? imageFile)
        {
            if (string.IsNullOrEmpty(menuItem.MenuItemId))
                menuItem.MenuItemId = id;

            if (id != menuItem.MenuItemId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["MenuCategoryId"] = new SelectList(db.MenuCategories, "MenuCategoryId", "Name", menuItem.MenuCategoryId);
                return View(menuItem);
            }

            try
            {
                db.Update(menuItem);
                await db.SaveChangesAsync();

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploads = Path.Combine(env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    var existingImage = await db.MenuItemImages.FirstOrDefaultAsync(i => i.MenuItemId == menuItem.MenuItemId);
                    if (existingImage != null)
                    {
                        existingImage.Url = "/uploads/" + fileName;
                        existingImage.SortOrder = 1;
                        db.MenuItemImages.Update(existingImage);
                    }
                    else
                    {
                        db.MenuItemImages.Add(new MenuItemImage
                        {
                            ImageId = "IM" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                            MenuItemId = menuItem.MenuItemId,
                            Url = "/uploads/" + fileName,
                            SortOrder = 1
                        });
                    }
                    await db.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Menu item updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!db.MenuItems.Any(e => e.MenuItemId == menuItem.MenuItemId))
                    return NotFound();
                else
                    throw;
            }
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var item = await db.MenuItems.Include(m => m.Category).FirstOrDefaultAsync(m => m.MenuItemId == id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var item = await db.MenuItems.FindAsync(id);
            if (item != null)
            {
                db.MenuItems.Remove(item);
                await db.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Menu item deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ImportCSV(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using (var stream = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<MenuItemCsvModel>().ToList();

                var usedSortOrders = new Dictionary<int, HashSet<int>>();

                foreach (var r in records)
                {
                    bool exists = db.MenuItems.Any(x =>
                        x.Name == r.Name &&
                        x.MenuCategoryId == r.MenuCategoryId
                    );

                    if (exists) continue;

                    if (!usedSortOrders.ContainsKey(r.MenuCategoryId))
                        usedSortOrders[r.MenuCategoryId] = new HashSet<int>(
                            db.MenuItems
                              .Where(x => x.MenuCategoryId == r.MenuCategoryId)
                              .Select(x => x.SortOrder)
                        );

                    int nextSort = r.SortOrder;
                    while (usedSortOrders[r.MenuCategoryId].Contains(nextSort))
                    {
                        nextSort++;
                    }
                    usedSortOrders[r.MenuCategoryId].Add(nextSort);

                    var menuItem = new MenuItem
                    {
                        MenuItemId = "MI" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                        MenuCategoryId = r.MenuCategoryId,
                        Name = r.Name,
                        Price = r.Price,
                        SortOrder = nextSort,
                        Description = r.Description,
                        Recipe = r.Recipe,
                        IsAvailable = r.IsAvailable,
                        UpdatedAt = DateTime.UtcNow
                    };

                    db.MenuItems.Add(menuItem);
                    await db.SaveChangesAsync(); 

                    // Add image only after menuItem is saved
                    if (!string.IsNullOrWhiteSpace(r.PhotoURL))
                    {
                        db.MenuItemImages.Add(new MenuItemImage
                        {
                            ImageId = "IM" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper(),
                            MenuItemId = menuItem.MenuItemId,
                            Url = r.PhotoURL,
                            SortOrder = 1
                        });

                        await db.SaveChangesAsync(); 
                    }
                }
            }

            TempData["SuccessMessage"] = "CSV imported successfully (duplicates skipped, sort order auto-fixed).";
            return RedirectToAction("Index");
        }

    }