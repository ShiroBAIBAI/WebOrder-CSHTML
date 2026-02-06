using Microsoft.AspNetCore.Mvc;

public class SalesReportController : Controller
{
    private readonly DB _context;

    public SalesReportController(DB context)
    {
        _context = context;
    }

    public IActionResult Index(string filter = "all", DateTime? startDate = null, DateTime? endDate = null)
    {
        DateTime today = DateTime.Today;
        DateTime start = today;
        DateTime end = today;

        switch (filter)
        {
            case "today":
                start = today;
                end = today.AddDays(1).AddTicks(-1);
                break;

            case "7days":
                start = today.AddDays(-6);
                end = today.AddDays(1).AddTicks(-1);
                break;

            case "month":
                start = new DateTime(today.Year, today.Month, 1);
                end = start.AddMonths(1).AddTicks(-1);
                break;

            case "lastmonth":
                start = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                end = new DateTime(today.Year, today.Month, 1).AddTicks(-1);
                break;

            case "custom":
                if (startDate.HasValue && endDate.HasValue)
                {
                    start = startDate.Value;
                    end = endDate.Value.AddDays(1).AddTicks(-1);
                }
                else
                {
                    filter = "all"; // fallback
                }
                break;

            default: // all
                start = DateTime.MinValue;
                end = DateTime.MaxValue;
                break;
        }

        //  Query sales from OrderLines + Orders
        var sales = _context.OrderLines
            .Where(l => l.Order.CreatedAt >= start && l.Order.CreatedAt <= end)
            .Select(l => new
            {
                Product = l.MenuItem.Name,
                Quantity = l.Quantity,
                Total = l.Quantity * l.Price,
                Date = l.Order.CreatedAt
            })
            .ToList();

        // Prepare chart/table data
        var labels = sales.Select(s => s.Product).Distinct().ToArray();
        var data = sales.GroupBy(s => s.Product).Select(g => g.Sum(x => x.Total)).ToArray();
        var totalSales = sales.Sum(s => s.Total);
        var mostPopular = sales.GroupBy(s => s.Product)
                               .OrderByDescending(g => g.Sum(x => x.Quantity))
                               .Select(g => g.Key)
                               .FirstOrDefault();

        ViewBag.Filter = filter;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.SalesData = sales;
        ViewBag.Labels = labels;
        ViewBag.Data = data;
        ViewBag.TotalSales = totalSales;
        ViewBag.MostPopular = mostPopular ?? "N/A";

        return View("SalesReport");
    }
}
