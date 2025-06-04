using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;

namespace WebApplication1.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartAppService _cartAppService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartAppService cartAppService, ILogger<CartController> logger)
    {
        _cartAppService = cartAppService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var sessionCart = await _cartAppService.GetCartAsync(HttpContext, User);
            return View(sessionCart.Items);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to cart");
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cart");
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> AddToCart(int id)
    {
        try
        {
            await _cartAppService.AddToCartAsync(id, HttpContext, User);
            return Json(new { message = "Item added successfully", IsError = false });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error adding item {ItemId} to cart", id);
            return Json(new { message = ex.Message, IsError = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access adding item {ItemId} to cart", id);
            return Json(new { message = "Please log in to add items to cart", IsError = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding item {ItemId} to cart", id);
            return Json(new { message = "An error occurred", IsError = true });
        }
    }

    public async Task<IActionResult> RemoveFromCart(int id)
    {
        try
        {
            await _cartAppService.RemoveFromCartAsync(id, HttpContext, User);
            return RedirectToAction("Index");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access removing item {ItemId} from cart", id);
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item {ItemId} from cart", id);
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int itemId, int quantity)
    {
        try
        {
            await _cartAppService.UpdateQuantityAsync(itemId, quantity, HttpContext, User);
            return RedirectToAction("Index");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error updating quantity for item {ItemId}", itemId);
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access updating quantity for item {ItemId}", itemId);
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating quantity for item {ItemId}", itemId);
            TempData["Error"] = "An error occurred";
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> BuyNow(int id)
    {
        try
        {
            await _cartAppService.PrepareBuyNowAsync(id, HttpContext, User);
            return RedirectToAction("Payment", "Order");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error preparing buy now for item {ItemId}", id);
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access preparing buy now for item {ItemId}", id);
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error preparing buy now for item {ItemId}", id);
            TempData["Error"] = "An error occurred";
            return RedirectToAction("Index");
        }
    }

    public IActionResult Checkout()
    {
        return RedirectToAction("Payment", "Order");
    }
}