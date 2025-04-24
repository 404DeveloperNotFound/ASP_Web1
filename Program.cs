using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Middlewares;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddDbContext<Web1Context>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()
        ));

        // for auth using EF Identity
        //builder.Services.AddIdentity<IdentityUser, IdentityRole>()
        // .AddEntityFrameworkStores<Web1Context>()  // Use Web1Context here
        // .AddDefaultTokenProviders();


        //builder.Services.AddScoped<UserService>();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Home/AccessDenied";
            });

        builder.Services.AddControllersWithViews().AddNewtonsoftJson();
        builder.Services.AddDistributedMemoryCache(); // Enables in-memory storage for sessions
        builder.Services.AddScoped<CartService>();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(8);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });


        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
        app.UseRouting();

        app.UseAuthentication();
        app.UseSession();
        app.UseMiddleware<BlacklistMiddleware>();
        app.UseAuthorization();
        
        app.MapStaticAssets();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();
    }
}