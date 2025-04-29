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
            if (existingItem.Quantity >= item.Quantity)
            {
                return Json(new
                {
                    message = $"Cannot add more. Only {item.Quantity} available in stock",
                    IsError = true
                });
            }
            existingItem.Quantity++;
        }
        else
        {
            sessionCart.Items.Add(new CartItemDto
            {
                ItemId = id,
                Name = item.Name,
                Quantity = 1,
                Price = (decimal)item.Price,
                MaxQuantity = item.Quantity
            });
        }
        HttpContext.Session.SetObject("Cart", sessionCart);

        return Json(new { message = $"{item.Name} added successfully", IsError = false });
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
        var dbItem = _context.Items.Find(itemId);

        if (item == null || dbItem == null) return NotFound();
       
        var newQuantity = Math.Min(quantity, dbItem.Quantity);
        newQuantity = Math.Max(1, newQuantity); 
        
        item.Quantity = newQuantity;
        item.MaxQuantity = dbItem.Quantity; 
       
        HttpContext.Session.SetObject("Cart", sessionCart);

        return RedirectToAction("Index");
    }

    public IActionResult BuyNow(int id)
    {
        var item = _context.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart");

        if(sessionCart == null)
        {
            sessionCart = new SessionCart
            {
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ItemId = id, Name = item.Name, Quantity = 1, Price = (decimal)item.Price, MaxQuantity = item.Quantity }
                }
            };
        }
        else
        {
            var existingItem = sessionCart.Items.FirstOrDefault(i => i.ItemId == id);
            if(existingItem != null)
            {
                if (existingItem.Quantity < item.Quantity)
                {
                    existingItem.Quantity++;
                }
            }
            else
            {
                sessionCart.Items.Add(new CartItemDto
                {
                    ItemId = id,
                    Name = item.Name,
                    Quantity = 1,
                    Price = (decimal)item.Price,
                    MaxQuantity = item.Quantity
                });
            }
        }

        HttpContext.Session.SetObject("Cart", sessionCart);
        return RedirectToAction("Payment", "Order");
    }

    public IActionResult Checkout()
    {
        return RedirectToAction("Payment", "Order");
    }
}