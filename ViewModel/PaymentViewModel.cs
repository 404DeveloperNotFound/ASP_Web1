using WebApplication1.DataTransferObjects;
using WebApplication1.Models;

namespace WebApplication1.ViewModel
{
    public class PaymentViewModel
    {
        public List<CartItemDto> CartItems { get; set; }
        public decimal TotalAmount { get; set; }
    }

}
