using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ValidationAttributes
{
    public class FutureOrCurrentMonth : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                var now = DateTime.Now;
                if (date.Year < now.Year || (date.Year == now.Year && date.Month < now.Month))
                {
                    return new ValidationResult(ErrorMessage ?? "Date must be this month or in the future.");
                }
            }
            return ValidationResult.Success;
        }
    }
}