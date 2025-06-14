﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AccountService : IAccountService
    {
        private readonly Web1Context _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AccountService(Web1Context context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        } 

        public async Task<AuthenticatedUserDto> RegisterAsync(RegisterDto dto)
        {
            if (_context.Clients.Any(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email is already taken.");

            if (_context.Clients.Any(u => u.Username == dto.Username))
                throw new InvalidOperationException("Username is already taken.");

            var hashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var otp = new Random().Next(100000, 999999).ToString();

            var user = new Client
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hashed,
                Role = dto.Role ?? "Client",
                IsEmailVerified = false,
                EmailOtp = otp,
                OtpGeneratedAt = DateTime.UtcNow
            };

            _context.Clients.Add(user);
            await _context.SaveChangesAsync();

            return new AuthenticatedUserDto(user.Id, user.Username, user.Email, user.Role, otp);
        }

        public async Task<AuthenticatedUserDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Clients.SingleOrDefaultAsync(u => u.Email == dto.Email)
                       ?? throw new UnauthorizedAccessException("Invalid login attempt.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid login attempt.");

            if (!user.IsEmailVerified)
                throw new UnauthorizedAccessException("Email not verified.");

            return new AuthenticatedUserDto(user.Id, user.Username, user.Email, user.Role, "");
        }

        public List<Claim> BuildClaims(AuthenticatedUserDto user)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };
        }

        public async Task SignInAsync(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }
    }
}
