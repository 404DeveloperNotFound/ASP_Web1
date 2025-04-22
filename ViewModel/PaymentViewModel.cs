using WebApplication1.Models;

namespace WebApplication1.ViewModel
{
    public class PaymentViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public decimal TotalAmount { get; set; }
    }

}
