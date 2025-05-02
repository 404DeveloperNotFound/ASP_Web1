using System.Security.Claims;
using WebApplication1.DataTransferObjects;

namespace WebApplication1.Interfaces
{
    public interface IOrderService
    {
        Task<PaymentDto> GetPaymentAsync(HttpContext httpContext);
        Task<int> ConfirmPaymentAsync(ConfirmPaymentDto dto, HttpContext httpContext, ClaimsPrincipal user);
        Task<OrderDto> GetOrderAsync(int id);
        Task<List<OrderDto>> GetUserOrdersAsync(ClaimsPrincipal user);
        Task<byte[]> GenerateInvoicePdfAsync(int id);
    }
}
