using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces;

public class CartController : Controller
{
    private readonly ICartAppService _cartAppService;

    public CartController(ICartAppService cartAppService)
    {
        _cartAppService = cartAppService;
    }

    public async Task<IActionResult> Index()
    {
        var sessionCart = await _cartAppService.GetSessionCartAsync(HttpContext);
        return View(sessionCart.Items);
    }

    public async Task<IActionResult> AddToCart(int id)
    {
        try
        {
            await _cartAppService.AddToCartAsync(id, HttpContext);
            return Json(new { message = "Item added successfully", IsError = false });
        }
        catch (Exception ex)
        {
            return Json(new { message = ex.Message, IsError = true });
        }
    }

    public async Task<IActionResult> RemoveFromCart(int id)
    {
        await _cartAppService.RemoveFromCartAsync(id, HttpContext);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int itemId, int quantity)
    {
        try
        {
            await _cartAppService.UpdateQuantityAsync(itemId, quantity, HttpContext);
            return RedirectToAction("Index");
        }
        catch
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> BuyNow(int id)
    {
        try
        {
            await _cartAppService.PrepareBuyNowAsync(id, HttpContext);
            return RedirectToAction("Payment", "Order");
        }
        catch
        {
            return NotFound();
        }
    }

    public IActionResult Checkout()
    {
        return RedirectToAction("Payment", "Order");
    }
}
