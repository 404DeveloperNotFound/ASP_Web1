namespace WebApplication1.DataTransferObjects;

public class SessionCart
{
    public List<CartItemDto> Items { get; set; } = new();
}

public class CartItemDto
{
    public int ItemId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    public int Quantity { get; set; }
    public int MaxQuantity { get; set; }
}