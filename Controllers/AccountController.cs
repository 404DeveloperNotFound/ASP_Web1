using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly CartService _cartService;
    private readonly RedisService _redisService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, CartService cartService, RedisService redisService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _cartService = cartService;
        _redisService = redisService;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid || model.Password != model.ConfirmPassword)
        {
            if (model.Password != model.ConfirmPassword)
                ModelState.AddModelError("", "Passwords do not match.");

            return View(model);
        }

        try
        {
            var dto = new RegisterDto(model.Username, model.Email, model.Password, model.Role);
            var user = await _accountService.RegisterAsync(dto);

            var claims = BuildClaims(user);
            await SignInAsync(claims);

            var sessionCart = await _cartService.LoadCartFromDbAsync(user.Id);
            await _redisService.SetAsync($"user:{user.Id}:cart", sessionCart, TimeSpan.FromMinutes(30));
            HttpContext.Session.SetObject("Cart", sessionCart);

            return RedirectToAction("Index", "Home");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return View("Error");
        }
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View();
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var dto = new LoginDto(model.Email, model.Password);
            var user = await _accountService.LoginAsync(dto);

            var claims = BuildClaims(user);
            await SignInAsync(claims);

            var cart = await _cartService.LoadCartFromDbAsync(user.Id);
            await _redisService.SetAsync($"user:{user.Id}:cart", cart, TimeSpan.FromMinutes(30));
            HttpContext.Session.SetObject("Cart", cart);

            return RedirectToAction("Index", "Home");
        }
        catch (UnauthorizedAccessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return View("Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var clientId))
            {
                var cacheKey = $"user:{clientId}:cart";
                var cart = await _redisService.GetAsync<SessionCart>(cacheKey) ?? new SessionCart();
                await _cartService.SaveCartToDbAsync(clientId, cart);
                await _redisService.RemoveAsync(cacheKey);
                HttpContext.Session.Remove("Cart");
                _logger.LogInformation("User {ClientId} logged out, cart saved to database", clientId);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return View("Error");
        }
    }

    private List<Claim> BuildClaims(AuthenticatedUserDto user)
    {
        return new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
    }

    private async Task SignInAsync(List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }
}