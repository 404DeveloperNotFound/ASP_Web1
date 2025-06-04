using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AccountService : IAccountService
    {
        private readonly Web1Context _context;

        public AccountService(Web1Context context)
        {
            _context = context;
        }

        public async Task<AuthenticatedUserDto> RegisterAsync(RegisterDto dto)
        {
            if (_context.Clients.Any(u => u.Email == dto.Email))
                throw new InvalidOperationException("Email is already taken.");

            if (_context.Clients.Any(u => u.Username == dto.Username))
                throw new InvalidOperationException("Username is already taken.");

            var hashed = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new Client
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hashed,
                Role = dto.Role ?? "Client"
            };

            _context.Clients.Add(user);
            await _context.SaveChangesAsync();

            return new AuthenticatedUserDto(user.Id, user.Username, user.Email, user.Role);
        }

        public async Task<AuthenticatedUserDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Clients.SingleOrDefaultAsync(u => u.Email == dto.Email)
                       ?? throw new UnauthorizedAccessException("Invalid login attempt.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid login attempt.");

            return new AuthenticatedUserDto(user.Id, user.Username, user.Email, user.Role);
        }
    }
}