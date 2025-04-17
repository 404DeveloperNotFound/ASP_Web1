using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using WebApplication1.Data;
using WebApplication1.Models;

[Authorize]
public class OrderController : Controller
{
    private readonly Web1Context _context;

    public OrderController(Web1Context context)
    {
        _context = context;
    }

    public IActionResult Payment()
    {
        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");
        if (addressId == null)
        {
            return RedirectToAction("Select", "Address", new { returnUrl = "/Order/Payment" });
        }

        var address = _context.Addresses.Find(addressId.Value);
        ViewBag.SelectedAddress = address;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPayment()
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");
        var clientIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(clientIdStr, out var clientId) || cart == null || !cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // populating items 
        foreach (var item in cart)
        {
            item.Item = await _context.Items.FindAsync(item.ItemId);
        }

        var order = new Order
        {
            ClientId = clientId,
            Items = cart.Select(c => new OrderItem
            {
                ItemId = c.ItemId,
                Quantity = c.Quantity,
                Price = (decimal)c.Item.Price
            }).ToList(),
            Status = "Placed"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        HttpContext.Session.Remove("Cart");
        return RedirectToAction("OrderPlaced", new { id = order.Id });
    }

    public IActionResult OrderPlaced(int id)
    {
        var order = _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Item)
            .FirstOrDefault(o => o.Id == id);

        return View(order);
    }

    public IActionResult MyOrders()
    {
        var clientIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clientIdStr, out var clientId))
        {
            return RedirectToAction("Index", "Home");
        }

        var orders = _context.Orders
            .Where(o => o.ClientId == clientId)
            .Include(o => o.Items)
            .ThenInclude(i => i.Item)
            .ToList();

        return View(orders);
    }

    public IActionResult DownloadInvoice(int id)
    {
        return File(Encoding.UTF8.GetBytes("Dummy invoice for order " + id), "text/plain", "Invoice.txt");
    }
}
