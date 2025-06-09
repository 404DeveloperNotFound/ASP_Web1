using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.AbstractClasses;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.ViewModels;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly CartService _cartService;
    private readonly ILogger<AccountController> _logger;
    private readonly IEmailService _emailService;
    private readonly Web1Context _context;

    public AccountController(IAccountService accountService, CartService cartService,
        ILogger<AccountController> logger, IEmailService emailService, Web1Context context)
    {
        _accountService = accountService;
        _cartService = cartService;
        _logger = logger;
        _emailService = emailService;
        _context = context;
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

            await _emailService.SendEmailAsync(user.Email, "Verify your email",
                 $"<h2>Your OTP is: {user.EmailOtp}</h2> <br/><p>It will expire after 5 minutes.</p>");

            TempData["UserEmail"] = user.Email;
            return RedirectToAction("VerifyEmail");
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
            var user = await _context.Clients.SingleOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid login attempt.");

            // verification of email
            if (!user.IsEmailVerified && !(user?.Role=="Admin"))
            {
                var otp = new Random().Next(100000, 999999).ToString();
                user.EmailOtp = otp;
                user.OtpGeneratedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(user.Email, "Verify your email",
                    $"<h2>Your OTP is: {otp}</h2> <br/><p>It will expire after 5 minutes.</p>");

                TempData["UserEmail"] = user.Email;
                return RedirectToAction("VerifyEmail");
            }

            // Email verified -> login
            var claims = BuildClaims(new AuthenticatedUserDto(user.Id, user.Username, user.Email, user.Role, user?.EmailOtp ?? ""));
            await SignInAsync(claims);

            var cart = await _cartService.LoadCartFromDbAsync(user.Id.ToString());
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

    [HttpGet, AllowAnonymous]
    public IActionResult VerifyEmail()
    {
        return View();
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(string otp)
    {
        var email = TempData["UserEmail"] as string;
        if (string.IsNullOrEmpty(email))
            return View("Error");

        var user = await _context.Clients.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || user.EmailOtp != otp || user.OtpGeneratedAt == null ||
            (DateTime.UtcNow - user.OtpGeneratedAt.Value).TotalMinutes > 5)
        {
            ModelState.AddModelError("", "Invalid or expired OTP.");
            TempData["UserEmail"] = email;
            return View();
        }

        user.IsEmailVerified = true;
        user.EmailOtp = null;
        user.OtpGeneratedAt = null;
        await _context.SaveChangesAsync();

        return RedirectToAction("Login");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp()
    {
        var email = TempData["UserEmail"] as string ?? Request.Cookies["UserEmail"];
        if (string.IsNullOrEmpty(email))
            return BadRequest("Email not found.");

        var user = await _context.Clients.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return NotFound("User not found.");

        var otp = new Random().Next(100000, 999999).ToString();
        user.EmailOtp = otp;
        user.OtpGeneratedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _emailService.SendEmailAsync(user.Email, "Verify your email",
            $"<h2>Your OTP is: {otp}</h2><p>It will expire after 5 minutes.</p>");

        Response.Cookies.Append("UserEmail", email);
        return Ok("OTP resent successfully.");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var sessionCart = HttpContext.Session.GetObject<SessionCart>("Cart");
                if (sessionCart != null)
                    await _cartService.SaveCartToDbAsync(userId, sessionCart);
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
