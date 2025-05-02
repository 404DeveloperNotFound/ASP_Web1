using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using IronPdf;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Models;
using WebApplication1.Interfaces;

namespace WebApplication1.Services;

public class OrderService : IOrderService
{
    private readonly Web1Context _context;

    public OrderService(Web1Context context)
    {
        _context = context;
    }

    public async Task<PaymentDto> GetPaymentAsync(HttpContext httpContext)
    {
        var addressId = httpContext.Session.GetInt32("SelectedAddressId")
                        ?? throw new InvalidOperationException("No address selected.");

        var address = await _context.Addresses.FindAsync(addressId)
                      ?? throw new KeyNotFoundException("Address not found.");

        var cart = httpContext.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
        var total = cart.Items.Sum(c => c.Quantity * c.Price);

        var addressDto = new AddressDto(
            address.Id,
            address.StreetAddress,
            address.City,
            address.State,
            address.Country,
            address.PostalCode,
            address.IsDefault
        );

        return new PaymentDto(cart.Items, total, addressDto);
    }

    public async Task<int> ConfirmPaymentAsync(ConfirmPaymentDto dto, HttpContext httpContext, ClaimsPrincipal user)
    {
        if (dto.CartItems == null || !dto.CartItems.Any())
            throw new InvalidOperationException("Cart is empty.");

        if (user.FindFirst(ClaimTypes.NameIdentifier)?.Value is not string uidStr
            || !int.TryParse(uidStr, out var clientId))
            throw new UnauthorizedAccessException("Invalid user.");

        // stock check
        var itemIds = dto.CartItems.Select(c => c.ItemId).ToList();
        var dbItems = await _context.Items.Where(i => itemIds.Contains(i.Id)).ToListAsync();

        foreach (var ci in dto.CartItems)
        {
            var db = dbItems.FirstOrDefault(i => i.Id == ci.ItemId)
                     ?? throw new KeyNotFoundException($"Item {ci.ItemId} not found.");
            if (db.Quantity < ci.Quantity)
                throw new InvalidOperationException($"Insufficient stock for {ci.Name}.");
        }

        // order
        var order = new Order
        {
            ClientId = clientId,
            Status = "Placed",
            Address = dto.Address,
            Items = dto.CartItems.Select(c => new OrderItem
            {
                ItemId = c.ItemId,
                Quantity = c.Quantity,
                Price = c.Price
            }).ToList()
        };

        // update stock
        foreach (var ci in dto.CartItems)
        {
            var db = dbItems.First(i => i.Id == ci.ItemId);
            db.Quantity -= ci.Quantity;
        }

        _context.Orders.Add(order);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Concurrency error: stock changed before order could be placed.");
        }

        httpContext.Session.Remove("Cart");
        return order.Id;
    }

    public async Task<OrderDto> GetOrderAsync(int id)
    {
        var o = await _context.Orders
            .Include(x => x.Address)
            .Include(x => x.Client)
            .Include(x => x.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Order not found.");

        var items = o.Items.Select(i => new OrderItemDto(
            i.ItemId,
            i.Item?.Name ?? "Unknown",
            i.Quantity,
            i.Price
        )).ToList();

        var total = items.Sum(i => i.Quantity * i.Price);

        var addr = o.Address;
        var addressDto = new AddressDto(
            addr.Id, addr.StreetAddress, addr.City,
            addr.State, addr.Country, addr.PostalCode, addr.IsDefault
        );

        return new OrderDto(o.Id, o.OrderDate, o.Status, addressDto, items, total);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(ClaimsPrincipal user)
    {
        if (user.FindFirst(ClaimTypes.NameIdentifier)?.Value is not string uidStr
            || !int.TryParse(uidStr, out var clientId))
            throw new UnauthorizedAccessException();

        var orders = await _context.Orders
            .Where(x => x.ClientId == clientId)
            .Include(x => x.Items)
            .ThenInclude(i => i.Item)
            .Include(x => x.Address)
            .ToListAsync();

        return orders.Select(o =>
        {
            var items = o.Items.Select(i => new OrderItemDto(
                i.ItemId,
                i.Item.Name,
                i.Quantity,
                (decimal)i.Price
            )).ToList();

            var totalAmount = items.Sum(i => i.Quantity * i.Price);

            return new OrderDto(
                o.Id,
                o.OrderDate,
                o.Status,
                new AddressDto(
                    o.Address.Id,
                    o.Address.StreetAddress,
                    o.Address.City,
                    o.Address.State,
                    o.Address.Country,
                    o.Address.PostalCode,
                    o.Address.IsDefault
                ),
                items,
                totalAmount
            );
        }).ToList();
    }


    public async Task<byte[]> GenerateInvoicePdfAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Address)
            .Include(o => o.Client)
            .Include(o => o.Items).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException("Order not found.");

        // HTML for invoice
        var sb = new StringBuilder();

        sb.Append($@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; }}
                h1 {{ text-align: center; }}
                table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                th {{ background-color: #f2f2f2; }}
                .total {{ font-weight: bold; }}
            </style>
        </head>
        <body>
            <h1>Invoice #{order.Id}</h1>
            <p><strong>Order Date:</strong> {order.OrderDate:yyyy-MM-dd}</p>
            <p><strong>Customer:</strong> {order.Client?.Username}</p>
            <p><strong>Status:</strong> {order.Status}</p>
            <h3>Shipping Address</h3>
            <p>
                {order.Address?.StreetAddress}<br />
                {order.Address?.City}, {order.Address?.State}<br />
                {order.Address?.Country} - {order.Address?.PostalCode}
            </p>

            <h3>Items</h3>
            <table>
                <thead>
                    <tr>
                        <th>Item</th>
                        <th>Serial Number</th>
                        <th>Price</th>
                        <th>Quantity</th>
                        <th>Subtotal</th>
                    </tr>
                </thead>
                <tbody>");

        foreach (var item in order.Items)
        {
            sb.Append($@"
            <tr>
                <td>{item.Item.Name}</td>
                <td>{item.Item.SerialNumber}</td>
                <td>{item.Price:C}</td>
                <td>{item.Quantity}</td>
                <td>{item.Price * item.Quantity:C}</td>
            </tr>");
        }

        var total = order.Items.Sum(i => i.Price * i.Quantity);


        sb.Append($@"
                </tbody>
                <tfoot>
                    <tr>
                        <td colspan='4' style='text-align:right;'><strong>Grand Total</strong></td>
                        <td ><strong>{total:C}</strong></td>
                    </tr>
                </tfoot>
            </table>

            <p style='margin-top: 40px;'>Thank you for your order!</p>
        </body>
        </html>");

        var html = sb.ToString();
        var renderer = new HtmlToPdf(); 
        return renderer.RenderHtmlAsPdf(html).BinaryData;
    }

}
