using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StripeNS = Stripe;
using StripeCheckout = Stripe.Checkout;

namespace Demo.Controllers;
    public class PaymentController : Controller
    {
        private readonly DB db;
        private readonly IConfiguration cfg;

        public PaymentController(DB db, IConfiguration cfg)
        {
            this.db = db;
            this.cfg = cfg;
        }

        [HttpGet("Payment/Start")]
        public async Task<IActionResult> Start(int orderId)
        {
            var order = await db.Orders
                .Include(o => o.Lines).ThenInclude(l => l.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            var lineItems = new List<StripeCheckout.SessionLineItemOptions>();
            foreach (var line in order.Lines)
            {
                long unitAmount = (long)(line.Price * 100m); 
                lineItems.Add(new StripeCheckout.SessionLineItemOptions
                {
                    Quantity = line.Quantity,
                    PriceData = new StripeCheckout.SessionLineItemPriceDataOptions
                    {
                        Currency = "myr",
                        UnitAmount = unitAmount,
                        ProductData = new StripeCheckout.SessionLineItemPriceDataProductDataOptions
                        {
                            Name = line.MenuItem?.Name ?? $"Item {line.MenuItemId}",
                            Description = string.IsNullOrWhiteSpace(line.Options) ? null : line.Options
                        }
                    }
                });
            }

            var domain = $"{Request.Scheme}://{Request.Host}";
            var opts = new StripeCheckout.SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                SuccessUrl = $"{domain}/Payment/Success?orderId={order.OrderId}",
                CancelUrl = $"{domain}/Payment/Cancel?orderId={order.OrderId}",
                Metadata = new Dictionary<string, string> { ["orderId"] = order.OrderId.ToString() }
            };

            var svc = new StripeCheckout.SessionService();
            var session = svc.Create(opts);
            return Redirect(session.Url);
        }

        [HttpPost("Payment/Checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int orderId)
        {
            return await Start(orderId);
        }

        public IActionResult Success(int orderId)
        {
            TempData["ok"] = $"Payment received for Order #{orderId}.";
            return RedirectToAction("OrderDetails", "Order", new { id = orderId });
        }

        public IActionResult Cancel(int orderId)
        {
            TempData["error"] = "Payment was cancelled.";
            return RedirectToAction("OrderDetails", "Order", new { id = orderId });
        }

        [HttpPost("Payment/Webhook")]
        public async Task<IActionResult> Webhook()
        {
            var payload = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var secret = cfg["Stripe:WebhookSecret"];
                var stripeEvent = StripeNS.EventUtility.ConstructEvent(
                    payload, Request.Headers["Stripe-Signature"], secret);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                }
                return Ok();
            }
            catch { return BadRequest(); }
        }
    }
