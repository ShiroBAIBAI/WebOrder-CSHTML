using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/menu")]           
[AllowAnonymous]           
public class MenuApiController : ControllerBase
{
    private readonly DB db;
    public MenuApiController(DB db) { this.db = db; }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search = null)
    {
        var q = db.MenuItems
                  .Include(m => m.Images)
                  .Include(m => m.Category)
                  .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            q = q.Where(m =>
                m.MenuItemId.Contains(search) ||
                m.Name.Contains(search) ||
                (m.Description != null && m.Description.Contains(search)) ||
                (m.Category != null && m.Category.Name.Contains(search))
            );
        }

        var items = await q
            .OrderBy(m => m.Category.SortOrder)
            .ThenBy(m => m.SortOrder)
            .Select(m => new {
                id = m.MenuItemId,
                name = m.Name,
                desc = m.Description,
                price = m.Price,
                images = m.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
                categoryId = m.MenuCategoryId,   
                categoryName = m.Category.Name,
                categorySort = m.Category.SortOrder
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/menu/items/MI0001
    [HttpGet("items/{id}")]
    public async Task<IActionResult> GetItem(string id)
    {
        var x = await db.MenuItems
                        .Include(m => m.Images)
                        .Include(m => m.Category)
                        .FirstOrDefaultAsync(m => m.MenuItemId == id && m.IsAvailable);
        if (x == null) return NotFound();

        return Ok(new
        {
            id = x.MenuItemId,
            name = x.Name,
            desc = x.Description,
            recipe = x.Recipe,
            price = x.Price,
            category = new { id = x.MenuCategoryId, name = x.Category.Name }, 
            tag = x.Tag,
            images = x.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
            updatedAt = x.UpdatedAt
        });
    }
}
