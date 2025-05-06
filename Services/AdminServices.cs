using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{ 
    public class AdminService : IAdminService
    {
        private readonly Web1Context _context;

        public AdminService(Web1Context context)
        {
            _context = context;  
        }
        public async Task<List<AdminItemDto>> GetAllItemsAsync()
        {
            return await _context.Items
                .Include(i => i.Category)
                .Select(i => new AdminItemDto(
                    i.Id,
                    i.Name,
                    (decimal)i.Price,
                    i.SerialNumber,
                    i.Category != null ? i.Category.Name : null,
                    i.Quantity
                ))
                .ToListAsync();
        }

        public async Task<List<AdminClientDto>> GetUsersAsync()
        {
            return await _context.Clients
                .Where(u => u.Role != "Admin")
                .Select(u => new AdminClientDto(u.Id, u.Username, u.Email, u.Role, u.IsBlocked))
                .ToListAsync();
        }

        public async Task<List<AdminClientDto>> GetAdminsAsync()
        {
            return await _context.Clients
                .Where(u => u.Role == "Admin")
                .Select(u => new AdminClientDto(u.Id, u.Username, u.Email, u.Role, u.IsBlocked))
                .ToListAsync();
        }

        public async Task PromoteUserToAdminAsync(int id)
        {
            var user = await _context.Clients.FindAsync(id) ?? throw new Exception("User not found.");
            user.Role = "Admin";
            await _context.SaveChangesAsync();
        }

        public async Task BlacklistUserAsync(int id)
        {
            var user = await _context.Clients.FindAsync(id) ?? throw new Exception("User not found.");
            user.IsBlocked = true;
            await _context.SaveChangesAsync();
        }

        public async Task UnBlacklistUserAsync(int id)
        {
            var user = await _context.Clients.FindAsync(id) ?? throw new Exception("User not found.");
            user.IsBlocked = false;
            await _context.SaveChangesAsync();
        }
    }
}
