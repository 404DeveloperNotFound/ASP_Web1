using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Services;

namespace WebApplication1.Services
{
    public class CartAppService : ICartAppService
    {
        private readonly Web1Context _context;
        private readonly RedisService _redisService;
        private readonly ILogger<CartAppService> _logger;
        private const int CacheExpiryMinutes = 30;

        public CartAppService(Web1Context context, RedisService redisService, ILogger<CartAppService> logger)
        {
            _context = context;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<SessionCart> GetCartAsync(HttpContext context, ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var clientId))
                throw new UnauthorizedAccessException("Invalid user.");

            var cacheKey = $"user:{clientId}:cart";
            var cachedCart = await _redisService.GetAsync<SessionCart>(cacheKey);
            if (cachedCart != null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return cachedCart;
            }

            // Fallback to session
            var sessionCart = context.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
            await _redisService.SetAsync(cacheKey, sessionCart, TimeSpan.FromMinutes(CacheExpiryMinutes));
            _logger.LogDebug("Cached cart for {CacheKey} from session", cacheKey);
            return sessionCart;
        }

        public async Task AddToCartAsync(int itemId, HttpContext context, ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var clientId))
                throw new UnauthorizedAccessException("Invalid user.");

            var cacheKey = $"user:{clientId}:cart";
            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == itemId)
                ?? throw new InvalidOperationException("Item not found");

            var cart = await GetCartAsync(context, user);
            var existingItem = cart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (existingItem != null)
            {
                if (existingItem.Quantity >= item.Quantity)
                    throw new InvalidOperationException($"Cannot add more. Only {item.Quantity} in stock.");
                existingItem.Quantity++;
                existingItem.MaxQuantity = item.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItemDto
                {
                    ItemId = itemId,
                    Name = item.Name,
                    Quantity = 1,
                    Price = (decimal)item.Price,
                    ImageUrl = item.ImageUrl,
                    MaxQuantity = item.Quantity
                });
            }

            await _redisService.SetAsync(cacheKey, cart, TimeSpan.FromMinutes(CacheExpiryMinutes));
            context.Session.SetObject("Cart", cart); 
            await _redisService.RemoveMatchingAsync("item:page:*");
            await _redisService.RemoveAsync("admin:items");
            _logger.LogInformation("Added item {ItemId} to cart for user {ClientId}", itemId, clientId);
        }

        public async Task RemoveFromCartAsync(int itemId, HttpContext context, ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var clientId))
                throw new UnauthorizedAccessException("Invalid user.");

            var cacheKey = $"user:{clientId}:cart";
            var cart = await GetCartAsync(context, user);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (item != null)
            {
                cart.Items.Remove(item);
                await _redisService.SetAsync(cacheKey, cart, TimeSpan.FromMinutes(CacheExpiryMinutes));
                context.Session.SetObject("Cart", cart); 
                await _redisService.RemoveMatchingAsync("item:page:*");
                await _redisService.RemoveAsync("admin:items");
                _logger.LogInformation("Removed item {ItemId} from cart for user {ClientId}", itemId, clientId);
            }
        }

        public async Task UpdateQuantityAsync(int itemId, int quantity, HttpContext context, ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var clientId))
                throw new UnauthorizedAccessException("Invalid user.");

            var cacheKey = $"user:{clientId}:cart";
            var dbItem = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId)
                         ?? throw new InvalidOperationException("Item not found");

            var cart = await GetCartAsync(context, user);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId)
                       ?? throw new InvalidOperationException("Item not in cart");

            if (quantity < 1)
                throw new InvalidOperationException("Quantity must be at least 1");

            if (quantity > dbItem.Quantity)
                throw new InvalidOperationException($"Cannot set quantity to {quantity}. Only {dbItem.Quantity} in stock.");

            item.Quantity = quantity;
            item.MaxQuantity = dbItem.Quantity;

            await _redisService.SetAsync(cacheKey, cart, TimeSpan.FromMinutes(CacheExpiryMinutes));
            context.Session.SetObject("Cart", cart); 
            await _redisService.RemoveMatchingAsync("item:page:*");
            await _redisService.RemoveAsync("admin:items");
            _logger.LogInformation("Updated quantity for item {ItemId} to {Quantity} for user {ClientId}", itemId, quantity, clientId);
        }

        public async Task PrepareBuyNowAsync(int itemId, HttpContext context, ClaimsPrincipal user)
        {
            if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var clientId))
                throw new UnauthorizedAccessException("Invalid user.");

            var cacheKey = $"user:{clientId}:cart";
            var item = await _context.Items
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == itemId)
                ?? throw new InvalidOperationException("Item not found");

            var cart = await GetCartAsync(context, user);
            var existingItem = cart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (existingItem != null)
            {
                if (existingItem.Quantity < item.Quantity)
                {
                    existingItem.Quantity++;
                    existingItem.MaxQuantity = item.Quantity;
                }
            }
            else
            {
                cart.Items.Add(new CartItemDto
                {
                    ItemId = itemId,
                    Name = item.Name,
                    Quantity = 1,
                    Price = (decimal)item.Price,
                    ImageUrl = item.ImageUrl,
                    MaxQuantity = item.Quantity
                });
            }

            await _redisService.SetAsync(cacheKey, cart, TimeSpan.FromMinutes(CacheExpiryMinutes));
            context.Session.SetObject("Cart", cart); 
            await _redisService.RemoveMatchingAsync("item:page:*");
            await _redisService.RemoveAsync("admin:items");
            _logger.LogInformation("Prepared buy now for item {ItemId} for user {ClientId}", itemId, clientId);
        }
    }
}