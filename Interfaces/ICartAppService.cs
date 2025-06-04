using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WebApplication1.DataTransferObjects;

namespace WebApplication1.Interfaces
{
    public interface ICartAppService
    {
        Task<SessionCart> GetCartAsync(HttpContext context, ClaimsPrincipal user);
        Task AddToCartAsync(int itemId, HttpContext context, ClaimsPrincipal user);
        Task RemoveFromCartAsync(int itemId, HttpContext context, ClaimsPrincipal user);
        Task UpdateQuantityAsync(int itemId, int quantity, HttpContext context, ClaimsPrincipal user);
        Task PrepareBuyNowAsync(int itemId, HttpContext context, ClaimsPrincipal user);
    }
}