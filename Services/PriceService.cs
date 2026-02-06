using Demo.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Demo.Services;

public class PriceService
{
    private readonly IConfiguration _cfg;
    private readonly DB _db;

    public PriceService(IConfiguration cfg, DB db)
    {
        _cfg = cfg;
        _db = db;
    }

    private decimal GetTaxRateFromDbOrDefault()
    {
        var s = _db.SystemSettings.FirstOrDefault(x => x.Key == "TaxRate");
        if (s != null && decimal.TryParse(s.Value, out var val))
            return val; // store as 0.06
        return DefaultTaxRate;
    }

    private IEnumerable<VoucherDef> LoadVouchersFromDbOrDefault()
    {
        var list = _db.Vouchers
            .Where(v => v.Active)
            .Select(v => new VoucherDef
            {
                Code = v.Code,
                Type = v.Type,
                Amount = v.Amount,
                MinSpend = v.MinSpend,
                ExpireAt = v.ExpireAt,
                Active = v.Active
            })
            .ToList();

        if (list.Count > 0) return list;
        return LoadVouchers(); // fallback to appsettings
    }

    public decimal DefaultTaxRate => _cfg.GetValue<decimal?>("TaxRate") ?? 0.06m;

    private IEnumerable<VoucherDef> LoadVouchers()
    {
        var section = _cfg.GetSection("Vouchers");
        if (section.Exists())
            return section.Get<IEnumerable<VoucherDef>>() ?? Enumerable.Empty<VoucherDef>();

        return new[]
        {
            new VoucherDef{ Code="WELCOME10", Type="percent", Amount=10,  MinSpend=0,   Active=true },
            new VoucherDef{ Code="LESS5",    Type="fixed",   Amount=5m,   MinSpend=20,  Active=true },
        };
    }

    public PriceBreakdown Calculate(IDictionary<string, CartItem> cart, string? voucherCode, decimal? taxRate = null)
    {
        var items = cart.Values.ToList();
        var subtotal = items.Sum(i => i.Item.Price * i.Quantity);

        decimal discount = 0m;
        string? applied = null;
        var vouchers = LoadVouchersFromDbOrDefault();

        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var v = vouchers.FirstOrDefault(x =>
                x.Active &&
                string.Equals(x.Code, voucherCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (!x.ExpireAt.HasValue || x.ExpireAt.Value >= DateTime.UtcNow) &&
                (x.MinSpend ?? 0) <= subtotal
            );
            if (v != null)
            {
                if (string.Equals(v.Type, "percent", StringComparison.OrdinalIgnoreCase))
                    discount = Math.Round(subtotal * (v.Amount / 100m), 2, MidpointRounding.AwayFromZero);
                else
                    discount = Math.Round(v.Amount, 2, MidpointRounding.AwayFromZero);

                if (discount > subtotal) discount = subtotal;
                applied = v.Code.ToUpperInvariant();
            }
        }

        var rate = taxRate ?? GetTaxRateFromDbOrDefault(); var baseAmount = subtotal - discount;
        if (baseAmount < 0) baseAmount = 0;
        var tax = Math.Round(baseAmount * rate, 2, MidpointRounding.AwayFromZero);
        var total = baseAmount + tax;

        return new PriceBreakdown
        {
            Subtotal = Math.Round(subtotal, 2),
            Discount = discount,
            TaxRate = rate,
            Tax = tax,
            Total = Math.Round(total, 2),
            VoucherCode = applied
        };
    }
}
