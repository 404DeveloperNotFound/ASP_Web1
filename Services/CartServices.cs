using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;

namespace WebApplication1.Services
{
    public class CartService
    {
        private readonly Web1Context _context;
        private readonly ILogger<CartService> _logger;

        public CartService(Web1Context context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveCartToDbAsync(int clientId, SessionCart sessionCart)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == clientId);

            if (cart == null)
            {
                cart = new Cart { UserId = clientId };
                _context.Carts.Add(cart);
            }
            else
            {
                cart.Items.Clear();
            }

            foreach (var dto in sessionCart.Items)
            {
                cart.Items.Add(new CartItem
                {
                    CartId = cart.Id,
                    ItemId = dto.ItemId,
                    Quantity = dto.Quantity,
                    Price = dto.Price
                });
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved cart to database for user {ClientId}", clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cart to database for user {ClientId}", clientId);
                throw;
            }
        }

        public async Task<SessionCart> LoadCartFromDbAsync(int clientId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Item)
                .FirstOrDefaultAsync(c => c.UserId == clientId);

            if (cart == null)
            {
                _logger.LogDebug("No cart found in database for user {ClientId}", clientId);
                return new SessionCart();
            }

            var sessionCart = new SessionCart
            {
                Items = cart.Items.Select(ci => new CartItemDto
                {
                    ItemId = ci.ItemId,
                    Name = ci.Item?.Name ?? "Unknown",
                    Quantity = ci.Quantity, 
                    Price = ci.Price,
                    MaxQuantity = ci.Item?.Quantity ?? 0
                }).ToList()
            };

            _logger.LogInformation("Loaded cart from database for user {ClientId}", clientId);
            return sessionCart;
        }
    }
}