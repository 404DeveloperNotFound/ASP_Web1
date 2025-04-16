using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class CartController : Controller
    {
        private readonly Web1Context _context;

        public CartController(Web1Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>(); 
            return View(cart);
        }

        public IActionResult AddToCart(int id)
        {
            var item = _context.Items.Find(id);
            if (item == null) return NotFound();

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var existingItem = cart.FirstOrDefault(i => i.ItemId == id);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem { ItemId = id, Item = item, Quantity = 1 });
            }
            HttpContext.Session.SetObject("Cart", cart);

            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var itemToRemove = cart.FirstOrDefault(i => i.ItemId == id);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetObject("Cart", cart);
            }

            return RedirectToAction("Index"); 
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int itemId, int quantity)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");
            var item = cart.FirstOrDefault(i => i.ItemId == itemId);

            if (item != null)
            {
                // minimum 1 item
                item.Quantity = Math.Max(1, quantity); 
                HttpContext.Session.SetObject("Cart", cart);
            }

            return RedirectToAction("Index");
        }

        public IActionResult BuyNow(int id)
        {
            var item = _context.Items.FirstOrDefault(i => i.Id == id);
            if (item == null) return NotFound();

            var cart = new List<CartItem>
            {
                new CartItem { ItemId = id, Item = item, Quantity = 1 }
            };

            HttpContext.Session.SetObject("Cart", cart);
            return RedirectToAction("Payment", "Order");
        }

        public IActionResult Checkout()
        {
            return RedirectToAction("Payment", "Order");
        }
    }
}
