using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using WebApplication1.DataTransferObjects;
using WebApplication1.Models;

namespace WebApplication1.ViewModel
{
    public class PaymentViewModel
    {
        [ValidateNever]
        public List<CartItemDto> CartItems { get; set; }
        [ValidateNever]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Card number must be exactly 12 digits")]
        [Display(Name = "Credit Card Number")]
        public string CreditCardNumber { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "Expiry date must be in YYYY-MM format")]
        [Display(Name = "Expiry Date")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV is required")]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "CVV must be exactly 3 digits")]
        [Display(Name = "CVV")]
        public string CVV { get; set; }
    }

}
