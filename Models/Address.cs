using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Address
    {
        public int Id { get; set; }
        [Required]
        public string StreetAddress { get; set; } = null!;
        [Required]
        public string City { get; set; } = null!;
        [Required]
        public string State { get; set; } = null!;
        [Required]
        public string Country { get; set; } = null!;
        [Required]
        public string PostalCode { get; set; } = null!;
        public bool IsDefault { get; set; } = false;

        public int? ClientId { get; set; }
        public Client? Client { get; set; }
    }
}