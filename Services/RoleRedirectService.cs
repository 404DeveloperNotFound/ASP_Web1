using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{
    public class RoleRedirectService : IRoleRedirectService
    {
        public IActionResult GetRedirectForUser(ClaimsPrincipal user, Controller controller)
        {
            if (user.IsInRole("Admin"))
                return controller.RedirectToAction("Index", "Admin");

            if (user.IsInRole("Client"))
                return controller.RedirectToAction("Index", "Item");

            return controller.View();
        } 
    }

}
