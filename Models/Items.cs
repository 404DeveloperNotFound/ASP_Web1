using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public interface IProduct 
    {
        int Id { get; set; }
        string Name { get; set; }
        double Price { get; set; }
    
    }

    public class Items : IProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Price { get; set; }
        public string? SerialNumber { get; set; }
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public List<Client>? Clients { get; set; }

        //public List<ItemClient>? ItemClients { get; set; }
    }
}
