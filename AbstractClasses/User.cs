using System.ComponentModel.DataAnnotations;

namespace WebApplication1.AbstractClasses
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
}
