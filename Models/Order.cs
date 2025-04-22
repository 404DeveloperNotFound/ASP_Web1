using System.Globalization;

namespace WebApplication1.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int ItemId { get; set; }
        public Items Item { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
    public class Order
    {
        public int Id { get; set; }
        public List<OrderItem> Items { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Processing";
        public int ClientId { get; set; }
        public Client Client { get; set; }
        public Address Address { get; set; }
    }
}
