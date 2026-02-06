using System.Text;
using Demo.Models;

namespace Demo.Services;

public static class ReceiptTemplate
{
    public static string BuildHtml(Order order, PriceBreakdown? pb = null)
    {
        var sb = new StringBuilder();
        sb.Append($@"
        <div style='font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#111'>
          <h2 style='margin:0 0 8px'>Thanks for your order!</h2>
          <div>Order <b>#{order.OrderId}</b> ¡¤ {order.CreatedAt:yyyy-MM-dd HH:mm}</div>
          <div style='margin:8px 0 16px'>Customer: <b>{order.MemberEmail}</b></div>
          <table style='border-collapse:collapse;width:100%'>
            <thead>
              <tr>
                <th style='text-align:left;border-bottom:1px solid #ddd;padding:8px'>Item</th>
                <th style='text-align:right;border-bottom:1px solid #ddd;padding:8px'>Qty</th>
                <th style='text-align:right;border-bottom:1px solid #ddd;padding:8px'>Price</th>
                <th style='text-align:right;border-bottom:1px solid #ddd;padding:8px'>Subtotal</th>
              </tr>
            </thead>
            <tbody>");
        foreach (var l in order.Lines)
        {
            var name = l.MenuItem?.Name ?? l.MenuItemId;
            var sub = l.Price * l.Quantity;
            sb.Append($@"
              <tr>
                <td style='padding:8px;border-bottom:1px solid #f0f0f0'>{name}</td>
                <td style='padding:8px;text-align:right;border-bottom:1px solid #f0f0f0'>{l.Quantity}</td>
                <td style='padding:8px;text-align:right;border-bottom:1px solid #f0f0f0'>RM {l.Price:0.00}</td>
                <td style='padding:8px;text-align:right;border-bottom:1px solid #f0f0f0'>RM {sub:0.00}</td>
              </tr>");
        }
        sb.Append("</tbody><tfoot>");
        if (pb != null)
        {
            sb.Append($@"
              <tr><td colspan='3' style='text-align:right;padding:10px 8px'>Subtotal</td>
                  <td style='text-align:right;padding:10px 8px'>RM {pb.Subtotal:0.00}</td></tr>
              <tr><td colspan='3' style='text-align:right;padding:10px 8px'>Voucher {(string.IsNullOrEmpty(pb.VoucherCode) ? "" : $"({pb.VoucherCode})")}</td>
                  <td style='text-align:right;padding:10px 8px'>- RM {pb.Discount:0.00}</td></tr>
              <tr><td colspan='3' style='text-align:right;padding:10px 8px'>Tax ({pb.TaxRate * 100m}%)</td>
                  <td style='text-align:right;padding:10px 8px'>RM {pb.Tax:0.00}</td></tr>");
        }
        sb.Append($@"
              <tr><td colspan='3' style='text-align:right;padding:10px 8px'><b>Total</b></td>
                  <td style='text-align:right;padding:10px 8px'><b>RM {(pb?.Total ?? order.Total):0.00}</b></td></tr>
            </tfoot></table>
          <p style='margin-top:16px;color:#666'>Attached is your PDF e-receipt. Keep this for your records.</p>
        </div>");
        return sb.ToString();
    }
}
