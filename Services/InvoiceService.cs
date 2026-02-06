using Demo.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Demo.Services;

public class InvoiceService
{
    public byte[] Generate(Order order, PriceBreakdown? pb = null)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text("E-Receipt").SemiBold().FontSize(20);
                    col.Item().Text($"Order #{order.OrderId}");
                    col.Item().Text($"Date: {order.CreatedAt:yyyy-MM-dd HH:mm}");
                    col.Item().Text($"Customer: {order.MemberEmail}");
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(6);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Item").SemiBold();
                        h.Cell().Element(HeaderCell).Text("Qty").SemiBold();
                        h.Cell().Element(HeaderCell).Text("Price").SemiBold();
                        h.Cell().Element(HeaderCell).Text("Subtotal").SemiBold();
                    });

                    foreach (var l in order.Lines)
                    {
                        table.Cell().Element(Cell).Text(l.MenuItem?.Name ?? l.MenuItemId);
                        table.Cell().Element(Cell).Text(l.Quantity.ToString());
                        table.Cell().Element(Cell).Text($"RM {l.Price:0.00}");
                        table.Cell().Element(Cell).Text($"RM {(l.Price * l.Quantity):0.00}");
                    }
                });

                page.Footer().Column(c =>
                {
                    if (pb != null)
                    {
                        c.Item().AlignRight().Text($"Subtotal: RM {pb.Subtotal:0.00}");
                        c.Item().AlignRight().Text($"Voucher {(string.IsNullOrEmpty(pb.VoucherCode) ? "" : $"({pb.VoucherCode})")}: - RM {pb.Discount:0.00}");
                        c.Item().AlignRight().Text($"Tax ({pb.TaxRate * 100m}%): RM {pb.Tax:0.00}");
                        c.Item().AlignRight().Text($"Total: RM {pb.Total:0.00}").SemiBold().FontSize(12);
                    }
                    else
                    {
                        c.Item().AlignRight().Text($"Total: RM {order.Total:0.00}").SemiBold().FontSize(12);
                    }
                });
            });
        });
        return doc.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer c) => c.BorderBottom(1).PaddingVertical(4);
    private static IContainer Cell(IContainer c) => c.PaddingVertical(4);
}
