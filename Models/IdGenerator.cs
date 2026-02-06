using Microsoft.EntityFrameworkCore;

namespace Demo.Models;

public static class Ids
{
    private static async Task<string> NextAsync(DB db, IQueryable<string> source, string prefix, int width = 4)
    {
        var last = await source
            .Where(s => s.StartsWith(prefix))
            .OrderByDescending(s => s)
            .FirstOrDefaultAsync();

        var n = 0;
        if (!string.IsNullOrEmpty(last))
        {
            var numPart = last.Substring(prefix.Length);
            int.TryParse(numPart, out n);
        }
        n += 1;
        return $"{prefix}{n.ToString($"D{width}")}";
    }

    public static Task<string> NextMenuItemIdAsync(DB db)
        => NextAsync(db, db.MenuItems.Select(x => x.MenuItemId), "MI");

    public static async Task<int> NextMenuCategoryIdAsync(DB db)
    {
        var lastId = await db.MenuCategories.MaxAsync(x => (int?)x.MenuCategoryId) ?? 0;
        return lastId + 1;
    }

    public static Task<string> NextImageIdAsync(DB db)
        => NextAsync(db, db.MenuItemImages.Select(x => x.ImageId), "IM");
}
