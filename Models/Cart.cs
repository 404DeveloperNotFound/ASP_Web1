using WebApplication1.Models;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; } // Match type with Client.Id (int)
    public Client User { get; set; }
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; } // Foreign key to Cart
    public Cart Cart { get; set; }
    public int ItemId { get; set; }
    public Items Item { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}