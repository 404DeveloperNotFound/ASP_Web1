using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApplication1.Interfaces
{
    public interface IRoleRedirectService
    {
        IActionResult GetRedirectForUser(ClaimsPrincipal user, Controller controller);
    }
}
