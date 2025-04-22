namespace WebApplication1.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
    }
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ClientId { get; set; }
        public Cart Cart { get; set; }
        public int ItemId { get; set; }
        public Items Item { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
