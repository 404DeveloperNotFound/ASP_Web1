using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Services;
using WebApplication1.ViewModel;

namespace WebApplication1.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ICartAppService _cartAppService;
    private readonly ILogger<OrderController> _logger;
    private readonly Web1Context _context;

    public OrderController(IOrderService orderService, ICartAppService cartService, ILogger<OrderController> logger, Web1Context context)
    {
        _orderService = orderService;
        _cartAppService = cartService;
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Payment()
    {
        try
        {
            var payment = await _orderService.GetPaymentAsync(HttpContext);
            ViewBag.SelectedAddress = payment.SelectedAddress;
            return View(new PaymentViewModel
            {
                CartItems = payment.CartItems,
                TotalAmount = payment.TotalAmount
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No address selected for payment");
            return RedirectToAction("Select", "Address", new { returnUrl = "/Order/Payment" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Address not found for payment");
            return RedirectToAction("Select", "Address", new { returnUrl = "/Order/Payment" });
        }
        catch (Exception ex)
        { 
            _logger.LogError(ex, "Error preparing payment");
            return RedirectToAction("Select", "Address", new { returnUrl = "/Order/Payment" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPayment(PaymentViewModel vm)
    {
        try
        {
            var addressId = HttpContext.Session.GetInt32("SelectedAddressId")
                           ?? throw new InvalidOperationException("No address selected.");

            var address = await _context.Addresses.FindAsync(addressId)
                           ?? throw new KeyNotFoundException("Selected address not found.");

            var sessionCart = await _cartAppService.GetCartAsync(HttpContext, User);
            if(sessionCart == null) sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
            var cartItems = sessionCart.Items;

            var dto = new ConfirmPaymentDto(cartItems, vm.TotalAmount, address);

            var orderId = await _orderService.ConfirmPaymentAsync(dto, HttpContext, User);
            return RedirectToAction("OrderPlaced", new { id = orderId });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during payment confirmation");
            ModelState.AddModelError("", ex.Message);
            return View("Payment", vm);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access during payment confirmation");
            return RedirectToAction("Login", "Account");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Key not found during payment confirmation: {Message}", ex.Message);
            ModelState.AddModelError("", ex.Message);
            return View("Payment", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment confirmation failed");
            ModelState.AddModelError("", "An error occurred while processing your payment.");
            return View("Payment", vm);
        }
    }

    public async Task<IActionResult> OrderPlaced(int id)
    {
        try
        {
            var order = await _orderService.GetOrderAsync(id);
            return View(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found, ID: {Id}", id);
            return NotFound($"Order {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {Id}", id);
            return NotFound();
        }
    }

    public async Task<IActionResult> MyOrders()
    {
        try
        {
            var orders = await _orderService.GetUserOrdersAsync(User);
            return View(orders);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to MyOrders");
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user orders");
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> DownloadInvoice(int id)
    {
        try
        {
            var pdfBytes = await _orderService.GenerateInvoicePdfAsync(id);
            return File(pdfBytes, "application/pdf", $"Invoice_Order_{id}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found for invoice, ID: {Id}", id);
            return NotFound($"Order {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for order {Id}", id);
            return NotFound();
        }
    }
}