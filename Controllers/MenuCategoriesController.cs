using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Demo.Models;

namespace Demo.Controllers;
    [Authorize(Roles = "Admin")]
    public class MenuCategoriesController : Controller
    {
        private readonly DB db;

        public MenuCategoriesController(DB db)
        {
            this.db = db;
        }

        public async Task<IActionResult> Index()
        {
            return View(await db.MenuCategories.ToListAsync());
        }
        public async Task<IActionResult> Details(int? id)  
        {
            if (id == null) return NotFound();

            var category = await db.MenuCategories
                .Include(c => c.MenuItems)  
                .FirstOrDefaultAsync(m => m.MenuCategoryId == id);

            if (category == null) return NotFound();

            return View(category);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuCategory category)
        {
            if (ModelState.IsValid)
            {
                db.Add(category);
                await db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Edit(int? id)  
        {
            if (id == null) return NotFound();
            var category = await db.MenuCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuCategory category)
        {
            if (id != category.MenuCategoryId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    db.Update(category);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenuCategoryExists(category.MenuCategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public async Task<IActionResult> Delete(int? id)  
        {
            if (id == null) return NotFound();

            var category = await db.MenuCategories
                .FirstOrDefaultAsync(m => m.MenuCategoryId == id);  

            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)   // ✅ int now
        {
            var category = await db.MenuCategories.FindAsync(id);
            if (category != null)
            {
                db.MenuCategories.Remove(category);
                await db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MenuCategoryExists(int id)  
        {
            return db.MenuCategories.Any(e => e.MenuCategoryId == id);
        }
    }

