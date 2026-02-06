
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;
    public class CartController : Controller
    {
        private readonly Helper hp;

        public CartController(Helper hp)
        {
            this.hp = hp;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Count()
        {
            int count = 0;

            try
            {
                var cart = hp.GetCart(); 
                if (cart != null)
                    count = cart.Values?.Sum(ci => ci?.Quantity ?? 0) ?? 0;
            }
            catch
            {

            }

            return Json(new { count });
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Cart", "Order");
        }
    }

