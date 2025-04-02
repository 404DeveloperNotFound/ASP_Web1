using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Items
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public double Price { get; set; }
        public int? SerialNumberId { get; set; }
        public SerialNumber? SerailNumber { get; set; }
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}
