using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;

public class CartController : Controller
{
    private readonly Web1Context _context;

    public CartController(Web1Context context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
        return View(sessionCart.Items);
    }

    public IActionResult AddToCart(int id)
    {
        var item = _context.Items.Find(id);
        if (item == null) return NotFound();

        var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
        var existingItem = sessionCart.Items.FirstOrDefault(i => i.ItemId == id);

        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            sessionCart.Items.Add(new CartItemDto
            {
                ItemId = id,
                Name = item.Name,
                Quantity = 1,
                Price = (decimal)item.Price
            });
        }
        HttpContext.Session.SetObject("Cart", sessionCart);

        return Json(new { message = "Item added successfully" });
    }

    public IActionResult RemoveFromCart(int id)
    {
        var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();

        var itemToRemove = sessionCart.Items.FirstOrDefault(i => i.ItemId == id);
        if (itemToRemove != null)
        {
            sessionCart.Items.Remove(itemToRemove);
            HttpContext.Session.SetObject("Cart", sessionCart);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult UpdateQuantity(int itemId, int quantity)
    {
        var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
        var item = sessionCart.Items.FirstOrDefault(i => i.ItemId == itemId);

        if (item != null)
        {
            item.Quantity = Math.Max(1, quantity);
            HttpContext.Session.SetObject("Cart", sessionCart);
        }

        return RedirectToAction("Index");
    }

    public IActionResult BuyNow(int id)
    {
        var item = _context.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        var sessionCart = new SessionCart
        {
            Items = new List<CartItemDto>
            {
                new CartItemDto { ItemId = id, Name = item.Name, Quantity = 1, Price = (decimal)item.Price }
            }
        };

        HttpContext.Session.SetObject("Cart", sessionCart);
        return RedirectToAction("Payment", "Order");
    }

    public IActionResult Checkout()
    {
        return RedirectToAction("Payment", "Order");
    }
}