using WebApplication1.AbstractClasses;

namespace WebApplication1.Models
{
    public class Client : User
    {
        public List<Order>? Orders { get; set; }

        public List<Address> Addresses { get; set; } = new();
    }
}
 