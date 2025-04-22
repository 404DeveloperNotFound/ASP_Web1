using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.DataTransferObjects;
using Microsoft.EntityFrameworkCore;

public class CartService
{
    private readonly Web1Context _context;

    public CartService(Web1Context context)
    {
        _context = context;
    }

    public async Task SaveCartToDbAsync(string userId, SessionCart cart)
    {
        int uid = int.Parse(userId);

        var existingItems = _context.CartItems.Where(c => c.ClientId == uid);
        _context.CartItems.RemoveRange(existingItems);

        foreach (var item in cart.Items)
        {
            _context.CartItems.Add(new CartItem
            {
                ClientId = uid,
                ItemId = item.ItemId,
                Quantity = item.Quantity,
                Price = item.Price
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<SessionCart> LoadCartFromDbAsync(string userId)
    {
        int uid = int.Parse(userId);

        var items = await _context.CartItems
                                  .Include(i => i.Item)
                                  .Where(c => c.ClientId == uid)
                                  .ToListAsync();

        return new SessionCart
        {
            Items = items.Select(i => new CartItemDto
            {
                ItemId = i.ItemId,
                Quantity = i.Quantity,
                Price = i.Price,
                Name = i.Item?.Name ?? "Unknown"
            }).ToList()
        };
    }
}
