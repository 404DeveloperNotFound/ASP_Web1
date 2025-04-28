using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using WebApplication1.Data;
using WebApplication1.Models;
using IronPdf;
using WebApplication1.ViewModel;
using WebApplication1.DataTransferObjects;
using WebApplication1.Migrations;

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

        var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();

        var totalAmount = sessionCart.Items.Sum(c => c.Quantity * c.Price);
        return View(new PaymentViewModel
        {
            CartItems = sessionCart.Items,
            TotalAmount = (decimal)totalAmount
        });
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPayment(PaymentViewModel model)
    {
        var cart = HttpContext.Session.GetObject<SessionCart>("Cart");
        model.CartItems = cart?.Items;
        model.TotalAmount = cart.Items.Sum(i => i.Quantity * i.Price);

        if (!ModelState.IsValid)
        {
            return View("Payment", model);
        }
        var clientIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(clientIdStr, out var clientId) || cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // Stock availability
        var itemIds = cart.Items.Select(c => c.ItemId).ToList();
        var dbItems = await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync();

        foreach (var cartItem in cart.Items)
        {
            var dbItem = dbItems.FirstOrDefault(i => i.Id == cartItem.ItemId);
            if (dbItem == null || dbItem.Quantity < cartItem.Quantity)
            {
                return BadRequest($"Insufficient stock for {cartItem?.Name ?? "item"}");
            }
        }

        // order creation
        var addressId = HttpContext.Session.GetInt32("SelectedAddressId");
        var address = addressId.HasValue ? await _context.Addresses.FindAsync(addressId.Value) : null;

        var order = new Order
        {
            ClientId = clientId,
            Items = cart.Items.Select(c => new OrderItem
            {
                ItemId = c.ItemId,
                Quantity = c.Quantity,
                Price = (decimal)c.Price
            }).ToList(),
            Status = "Placed",
            Address = address ?? new Address()
        };

        // Update quantity / stock
        foreach (var cartItem in cart.Items)
        {
            var dbItem = dbItems.First(i => i.Id == cartItem.ItemId);
            dbItem.Quantity -= cartItem.Quantity;
        }

        _context.Orders.Add(order);
        try
        {
            await _context.SaveChangesAsync();  // ← may throw DbUpdateConcurrencyException
        }
        catch (DbUpdateConcurrencyException)
        {
            // someone else just modified one of these rows
            TempData["ErrorMessage"] = 
                  "Sorry — one of the items in your cart just sold out before we could complete your order. Please review your cart.";
            return RedirectToAction("Index", "Cart");
        }

        HttpContext.Session.Remove("Cart");

        return RedirectToAction("OrderPlaced", new { id = order.Id });
    }

    public IActionResult OrderPlaced(int id)
    {
        var order = _context.Orders
            .Include(o => o.Address)
            .Include(o => o.Client)
            .Include(o => o.Items)
                .ThenInclude(i => i.Item)
            .FirstOrDefault(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

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
        var order = _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Item)
            .Include(o => o.Client)
            .Include(o => o.Address)
            .FirstOrDefault(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var htmlContent = GenerateInvoiceHtml(order);
        var renderer = new HtmlToPdf();
        var pdf = renderer.RenderHtmlAsPdf(htmlContent);
        var pdfBytes = pdf.BinaryData;

        return File(pdfBytes, "application/pdf", $"Invoice_Order_{id}.pdf");
    }

    private string GenerateInvoiceHtml(Order order)
    {
        var sb = new StringBuilder();

        sb.Append($@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; font-size: 14px; }}
                h1 {{ color: #2c3e50; }}
                table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                th {{ background-color: #f4f4f4; }}
                tfoot td {{ font-weight: bold; }}
            </style>
        </head>
        <body>
            <h1>Invoice - Order #{order.Id}</h1>
            <p><strong>Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
            <p><strong>Customer:</strong> {order.Client?.Username}</p>
            <p><strong>Address:</strong> {order.Address?.StreetAddress}, {order.Address?.City}, {order.Address?.State}, {order.Address?.PostalCode}</p>

            <table>
                <thead>
                    <tr>
                        <th>Item</th>
                        <th>Quantity</th>
                        <th>Price</th>
                        <th>Total</th>
                    </tr>
                </thead>
                <tbody>");

                decimal grandTotal = 0;

                foreach (var orderItem in order?.Items)
                {
                    var itemName = orderItem.Item?.Name ?? "Unnamed Item";
                    var quantity = orderItem.Quantity;
                    var price = orderItem.Price;
                    var total = quantity * price;

                    grandTotal += total;

                    sb.Append($@"
                    <tr>
                        <td>{itemName}</td>
                        <td>{quantity}</td>
                        <td>{price:C}</td>
                        <td>{total:C}</td>
                    </tr>");
                }

                sb.Append($@"
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan='3' style='text-align:right;'>Grand Total</td>
                        <td>{grandTotal:C}</td>
                    </tr>
                </tfoot>
            </table>

            <p style='margin-top: 40px;'>Thank you for your order!</p>
        </body>
        </html>");

        return sb.ToString();
    }
}
