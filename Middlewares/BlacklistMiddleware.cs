using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using WebApplication1.Data;

namespace WebApplication1.Middlewares
{
    public class BlacklistMiddleware
    {
        //private readonly Web1Context _context;        // can't do 
        private readonly RequestDelegate _next;

        public BlacklistMiddleware(RequestDelegate next)
        {
            //_context = context;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // it stops the infinite redirect to /Home/Blacklisted
            if (context.Request.Path.StartsWithSegments("/Home/Blacklisted", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
                var DBcontext = context.RequestServices.GetRequiredService<Web1Context>();
                var user = await DBcontext.Clients.FirstOrDefaultAsync(u => u.Email == email);

                if (user.IsBlocked)
                {
                    context.Response.Redirect("/Home/Blacklisted");
                    return;
                }
            }

            await _next(context);
        }
    }
}
