namespace WebApplication1.DataTransferObjects
{
    public class SessionCart
    {
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; } 
        public int MaxQuantity { get; set; }
    }

}
