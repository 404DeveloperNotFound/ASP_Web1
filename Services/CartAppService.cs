using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{
    public class CartAppService : ICartAppService
    {
        private readonly Web1Context _context;

        public CartAppService(Web1Context context)
        {
            _context = context;
        }

        public async Task<SessionCart> GetSessionCartAsync(HttpContext context)
        {
            return context.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
        }

        public async Task AddToCartAsync(int itemId, HttpContext context)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null) throw new InvalidOperationException("Item not found");

            var sessionCart = context.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
            var existingItem = sessionCart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (existingItem != null)
            {
                if (existingItem.Quantity >= item.Quantity)
                    throw new InvalidOperationException($"Cannot add more. Only {item.Quantity} in stock.");

                existingItem.Quantity++;
            }
            else
            {
                sessionCart.Items.Add(new CartItemDto
                {
                    ItemId = itemId,
                    Name = item.Name,
                    Quantity = 1,
                    Price = (decimal)item.Price,
                    MaxQuantity = item.Quantity
                });
            }

            context.Session.SetObject("Cart", sessionCart);
        }

        public async Task RemoveFromCartAsync(int itemId, HttpContext context)
        {
            var sessionCart = context.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
            var item = sessionCart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (item != null)
            {
                sessionCart.Items.Remove(item);
                context.Session.SetObject("Cart", sessionCart);
            }
        }

        public async Task UpdateQuantityAsync(int itemId, int quantity, HttpContext context)
        {
            var dbItem = await _context.Items.FindAsync(itemId);
            if (dbItem == null) throw new InvalidOperationException("Item not found");

            var sessionCart = context.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();
            var item = sessionCart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (item == null) throw new InvalidOperationException("Item not in cart");

            item.Quantity = Math.Clamp(quantity, 1, dbItem.Quantity);
            item.MaxQuantity = dbItem.Quantity;

            context.Session.SetObject("Cart", sessionCart);
        }

        public async Task PrepareBuyNowAsync(int itemId, HttpContext context)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null) throw new InvalidOperationException("Item not found");

            var sessionCart = context.Session.GetObject<SessionCart>("Cart") ?? new SessionCart();

            var existingItem = sessionCart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (existingItem != null)
            {
                if (existingItem.Quantity < item.Quantity)
                    existingItem.Quantity++;
            }
            else
            {
                sessionCart.Items.Add(new CartItemDto
                {
                    ItemId = itemId,
                    Name = item.Name,
                    Quantity = 1,
                    Price = (decimal)item.Price,
                    MaxQuantity = item.Quantity
                });
            }

            context.Session.SetObject("Cart", sessionCart);
        }
    }
}
