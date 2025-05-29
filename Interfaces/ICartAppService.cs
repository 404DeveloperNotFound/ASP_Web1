using WebApplication1.DataTransferObjects;

namespace WebApplication1.Interfaces
{
    public interface ICartAppService
    {
        SessionCart GetSessionCart(HttpContext httpContext);
        Task AddToCartAsync(int itemId, HttpContext httpContext);
        Task RemoveFromCartAsync(int itemId, HttpContext httpContext);
        Task UpdateQuantityAsync(int itemId, int quantity, HttpContext httpContext);
        Task PrepareBuyNowAsync(int itemId, HttpContext httpContext);
    }
}
