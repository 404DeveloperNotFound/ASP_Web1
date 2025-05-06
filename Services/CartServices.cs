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
     
    public async Task SaveCartToDbAsync(string userId, SessionCart sessionCart)
    {
        int uid = int.Parse(userId);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == uid);

        if (cart == null)
        {
            cart = new Cart { UserId = uid };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        cart.Items.Clear();

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

        await _context.SaveChangesAsync();
    }

    public async Task<SessionCart> LoadCartFromDbAsync(string userId)
    {
        int uid = int.Parse(userId);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(ci => ci.Item)
            .FirstOrDefaultAsync(c => c.UserId == uid);

        if (cart == null) return new SessionCart();

        return new SessionCart
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
    }
}
