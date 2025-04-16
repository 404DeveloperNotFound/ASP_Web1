using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public abstract class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        public bool IsBlocked { get; set; } = false;
        [Required]
        public string Role { get; set; } // "Admin" or "Client"
    }
    public class Client : User
    {
        public List<Order>? Orders { get; set; }
        //public List<Items>? Items { get; set; }

        //public List<ItemClient>? ItemClients { get; set; }
    }
}
