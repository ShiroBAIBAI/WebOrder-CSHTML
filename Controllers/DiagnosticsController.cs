using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.Controllers;
    [AllowAnonymous]
    [Route("diag")]
    public class DiagnosticsController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment env;

        public DiagnosticsController(DB db, IWebHostEnvironment env)
        {
            this.db = db;
            this.env = env;
        }

        [HttpGet("ping/{id}")]
        public IActionResult Ping(string id) => Content($"HIT id=[{id}]");

        [HttpGet("ids")]
        public async Task<IActionResult> Ids()
        {
            var ids = await db.MenuItems
                .OrderBy(x => x.MenuItemId)
                .Select(x => x.MenuItemId)
                .ToListAsync();
            return Content("IDs = [" + string.Join(", ", ids) + "]");
        }

        [HttpGet("dbinfo")]
        public IActionResult DbInfo()
        {
            var contentRoot = env.ContentRootPath;
            var dbPath = Path.Combine(contentRoot, "DB.mdf");
            var exists = System.IO.File.Exists(dbPath);
            return Content($"ContentRoot = {contentRoot}\nAttachDbFilename = {dbPath}\nExists? {exists}");
        }
    }

