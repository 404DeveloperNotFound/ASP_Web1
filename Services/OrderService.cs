using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using IronPdf;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{
    public class OrderService : IOrderService
    {
        private readonly Web1Context _context;
        private readonly RedisService _redisService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(Web1Context context, RedisService redisService, ILogger<OrderService> logger)
        {
            _context = context;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<PaymentDto> GetPaymentAsync(HttpContext httpContext)
        {
            var addressId = httpContext.Session.GetInt32("SelectedAddressId")
                            ?? throw new InvalidOperationException("No address selected.");

            var address = await _context.Addresses.FindAsync(addressId)
                          ?? throw new KeyNotFoundException("Address not found.");

            if (!int.TryParse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var clientId))
                throw new UnauthorizedAccessException("Invalid user.");

            var cacheKey = $"user:{clientId}:cart";
            var cart = await _redisService.GetAsync<SessionCart>(cacheKey)
                       ?? httpContext.Session.GetObject<SessionCart>("Cart")
                       ?? new SessionCart();

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

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Stock check
                    var itemIds = dto.CartItems.Select(c => c.ItemId).ToList();
                    var dbItems = await _context.Items
                        .Include(i => i.Category)
                        .Where(i => itemIds.Contains(i.Id))
                        .ToListAsync();

                    foreach (var ci in dto.CartItems)
                    {
                        var dbItem = dbItems.FirstOrDefault(i => i.Id == ci.ItemId)
                                     ?? throw new KeyNotFoundException($"Item {ci.ItemId} not found.");
                        if (dbItem.Quantity < ci.Quantity)
                            throw new InvalidOperationException($"Insufficient stock for {ci.Name}.");
                    }

                    // Create order
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

                    // Update stock in DB
                    foreach (var ci in dto.CartItems)
                    {
                        var dbItem = dbItems.First(i => i.Id == ci.ItemId);
                        dbItem.Quantity -= ci.Quantity;
                    }

                    _context.Orders.Add(order);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogWarning(ex, "Concurrency error placing order for user {ClientId}", clientId);
                        throw new InvalidOperationException("Concurrency error: stock changed before order could be placed.", ex);
                    }

                    // Update Redis
                    var tasks = new List<Task>();
                    foreach (var ci in dto.CartItems)
                    {
                        var dbItem = dbItems.First(i => i.Id == ci.ItemId);
                        var itemDto = new ItemDto(
                            dbItem.Id,
                            dbItem.Name,
                            (decimal)dbItem.Price,
                            dbItem.ImageUrl,
                            dbItem.CategoryId,
                            dbItem.Category?.Name ?? "",
                            dbItem.SerialNumber,
                            dbItem.Quantity,
                            dbItem.RowVersion
                        );
                        tasks.Add(_redisService.SetAsync($"item:{dbItem.Id}", itemDto));
                        tasks.Add(_redisService.SetAsync($"item:{dbItem.Id}:dto", itemDto));
                        tasks.Add(_redisService.SetAsync($"stock:{dbItem.Id}", dbItem.Quantity));
                    }

                    tasks.Add(_redisService.RemoveMatchingAsync("item:page:*"));
                    tasks.Add(_redisService.RemoveAsync("admin:items"));
                    tasks.Add(_redisService.RemoveAsync($"user:{clientId}:cart"));

                    await Task.WhenAll(tasks);

                    httpContext.Session.Remove("Cart");
                    await transaction.CommitAsync();
                    _logger.LogInformation("Order placed for user {ClientId}, updated item caches, cleared cart", clientId);

                    return order.Id;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error placing order for user {ClientId}", clientId);
                    throw;
                }
            });
        }

        public async Task<OrderDto> GetOrderAsync(int id)
        {
            var order = await _context.Orders
                .Include(x => x.Address)
                .Include(x => x.Client)
                .Include(x => x.Items).ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Order not found.");

            var items = order.Items.Select(i => new OrderItemDto(
                i.ItemId,
                i.Item?.Name ?? "Unknown",
                i.Quantity,
                i.Price
            )).ToList();

            var total = items.Sum(i => i.Quantity * i.Price);

            var addressDto = new AddressDto(
                order.Address.Id,
                order.Address.StreetAddress,
                order.Address.City,
                order.Address.State,
                order.Address.Country,
                order.Address.PostalCode,
                order.Address.IsDefault
            );

            return new OrderDto(order.Id, order.OrderDate, order.Status, addressDto, items, total);
        }

        public async Task<List<OrderDto>> GetUserOrdersAsync(ClaimsPrincipal user)
        {
            if (user.FindFirst(ClaimTypes.NameIdentifier)?.Value is not string uidStr
                || !int.TryParse(uidStr, out var clientId))
                throw new UnauthorizedAccessException();

            var orders = await _context.Orders
                .Where(x => x.ClientId == clientId)
                .Include(x => x.Items).ThenInclude(i => i.Item)
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
                    <td>{item.Item?.Name ?? "Unknown"}</td>
                    <td>{item.Item?.SerialNumber ?? "N/A"}</td>
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
                            <td><strong>{total:C}</strong></td>
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
}