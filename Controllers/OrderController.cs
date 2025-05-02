using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.ViewModel;

namespace WebApplication1.Controllers;

[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;
    private readonly Web1Context _context;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger, Web1Context context)
    {
        _orderService = orderService;
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
            var addressId = HttpContext.Session
                                   .GetInt32("SelectedAddressId")
                           ?? throw new InvalidOperationException("No address selected.");

            var address = await _context.Addresses
                                        .FindAsync(addressId)
                           ?? throw new KeyNotFoundException("Selected address not found.");

            var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
            
            var cartItems = sessionCart.Items;

            var dto = new ConfirmPaymentDto(
                cartItems,
                vm.TotalAmount,
                address   
            );

            var orderId = await _orderService.ConfirmPaymentAsync(dto, HttpContext, User);
            return RedirectToAction("OrderPlaced", new { id = orderId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Payment", vm);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Account");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment confirmation failed");
            return RedirectToAction("Index", "Cart");
        }
    }


    public async Task<IActionResult> OrderPlaced(int id)
    {
        try
        {
            var order = await _orderService.GetOrderAsync(id);
            return View(order);
        }
        catch
        {
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
        catch
        {
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
        catch
        {
            return NotFound();
        }
    }
}
