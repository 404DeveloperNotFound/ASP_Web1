using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{ 
    public class AdminService : IAdminService
    {
        private readonly Web1Context _context;
        private readonly RedisService _redisService;
        private readonly ILogger<AdminService> _logger;
        private const int CacheExpiryMinutes = 30;

        public AdminService(Web1Context context, RedisService redisService, ILogger<AdminService> logger)
        {
            _context = context;
            _redisService = redisService;
            _logger = logger;
        }
        public async Task<List<AdminItemDto>> GetAllItemsAsync()
        {
            const string cacheKey = "admin:items";

            // Try to get from cache
            var cachedItems = await _redisService.GetAsync<List<AdminItemDto>>(cacheKey);
            if (cachedItems != null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return cachedItems;
            }

            // Fetch from database
            var items = await _context.Items
                .Include(i => i.Category)
                .Select(i => new AdminItemDto(
                    i.Id,
                    i.Name,
                    (decimal)i.Price,
                    i.ImageUrl,
                    i.SerialNumber,
                    i.Category != null ? i.Category.Name : null,
                    i.Quantity
                ))
                .ToListAsync();

            // Cache the result
            await _redisService.SetAsync(cacheKey, items, TimeSpan.FromMinutes(CacheExpiryMinutes));
            _logger.LogDebug("Cached items for {CacheKey}", cacheKey);

            return items;
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
