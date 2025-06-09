using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WebApplication1.Data;
using WebApplication1.Interfaces;
using WebApplication1.Middlewares;
using WebApplication1.Services;

internal class Program
{
    private static async Task Main(string[] args)
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
        builder.Services.AddHostedService<RedisSyncBackgroundService>();
        builder.Services.AddDistributedMemoryCache(); // Enables in-memory storage for sessions
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "ECommerce_";
            options.ConnectionMultiplexerFactory = async () =>
            {
                var config = ConfigurationOptions.Parse("localhost:6379");
                config.AbortOnConnectFail = false;
                config.ConnectRetry = 3;
                config.ConnectTimeout = 5000;

                var redis = await ConnectionMultiplexer.ConnectAsync(config);

                redis.ConnectionFailed += (sender, args) =>
                {
                    Console.WriteLine($"Redis connection failed: {args.Exception}");
                };

                redis.ConnectionRestored += (sender, args) =>
                {
                    Console.WriteLine("Redis connection restored");
                };

                return redis;
            };
        });
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<CartService>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IAddressService, AddressService>();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<ICartAppService, CartAppService>();
        builder.Services.AddScoped<IRoleRedirectService, RoleRedirectService>();
        builder.Services.AddScoped<IItemService, ItemService>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddHostedService<RedisSyncBackgroundService>();
        // Redis services
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = ConfigurationOptions.Parse("localhost:6379");
            configuration.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configuration);
        });

        // Then register your RedisService
        builder.Services.AddSingleton<RedisService>();
        builder.Services.AddScoped<RedisInitializer>();
        builder.Services.AddScoped<RedisToDbSynchronizer>();

        // Application services (implement later)
        //builder.Services.AddScoped<ProductListingService>();
        //builder.Services.AddScoped<ShoppingCartService>();
        //builder.Services.AddScoped<StockService>();
        //builder.Services.AddScoped<OrderService>();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(8);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        License.LicenseKey = "IRONSUITE.OM.SINGH.CODITAS.COM.22579-139B8FB7D1-CGMH6-GSFNMEJHDIRP-2BWTWV2AKSXE-G5TL4XETKAEI-BFEFCWKIH5XM-TSEPVD4K73FC-57X7STGAYV6M-GZ5MMP-TQZ3D2H7AMSPUA-DEPLOYMENT.TRIAL-TMQH46.TRIAL.EXPIRES.02.JUL.2025";

        var app = builder.Build();

        // Initialize Redis data (only in development)
        if (app.Environment.IsDevelopment())
        {
            using (var scope = app.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<RedisInitializer>();
                try
                {
                    await initializer?.InitializeDataAsync();
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred initializing Redis data");
                }
            }
        }

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