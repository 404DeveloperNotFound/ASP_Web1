using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DataTransferObjects;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ItemService : IItemService
    {
        private readonly Web1Context _context;
        private readonly RedisService _redisService;
        private readonly ILogger _logger;
        private const int DefaultPageSize = 10;
        private const int CacheExpiryMinutes = 30;

        public ItemService(Web1Context context, RedisService redisService, ILogger<ItemService> logger)
        {
            _context = context;
            _redisService = redisService;
            _logger = logger;
        }

        public async Task<PaginatedList<ItemDto>> GetPagedItemsAsync(ItemQueryParameters parms)
        {
            var cacheKey = $"item:page:{parms.PageNumber}:size:{parms.PageSize}" +
                           $":search:{parms.Search}:cat:{parms.CategoryId}:sort:{parms.SortOrder}";

            // Get from Redis first
            var cachedResult = await _redisService.GetAsync<PaginatedList<ItemDto>>(cacheKey, parms);
            if (cachedResult != null)
            {
                _logger.LogDebug("Retrieved paginated items from cache for key {CacheKey}", cacheKey);
                return cachedResult;
            }

            // If not in cache, get from DB
            IQueryable<Items> items = _context.Items
                .Include(i => i.Category);

            // Filtering
            if (!string.IsNullOrEmpty(parms.Search))
            {
                items = items.Where(i =>
                    i.Name.Contains(parms.Search) ||
                    i.SerialNumber.Contains(parms.Search));
            }

            if (parms.CategoryId.HasValue)
            {
                items = items.Where(i => i.CategoryId == parms.CategoryId.Value);
            }

            // Sorting
            items = parms.SortOrder switch
            {
                "price_asc" => items.OrderBy(i => i.Price),
                "price_desc" => items.OrderByDescending(i => i.Price),
                "name_asc" => items.OrderBy(i => i.Name),
                "name_desc" => items.OrderByDescending(i => i.Name),
                _ => items.OrderBy(i => i.Name)
            };

            var result = await PaginatedList<ItemDto>
                .CreateAsync(
                    items.Select(i =>
                        new ItemDto
                        (
                            i.Id,
                            i.Name,
                            (decimal)i.Price,
                            i.ImageUrl,
                            i.CategoryId,
                            i.Category != null ? i.Category.Name : "",
                            i.SerialNumber,
                            i.Quantity,
                            i.RowVersion
                        )
                    ).AsNoTracking(),
                    parms.PageNumber > 0 ? parms.PageNumber : 1,
                    parms.PageSize);

            // Caching
            await _redisService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(CacheExpiryMinutes));
            _logger.LogDebug("Cached paginated items for key {CacheKey}", cacheKey);

            return result;
        }

        public async Task<ItemDto> GetByIdAsync(int id)
        {
            var cacheKey = $"item:{id}";

            // Get from Redis first
            var cachedItem = await _redisService.GetAsync<ItemDto>(cacheKey);
            if (cachedItem != null)
            {
                _logger.LogDebug("Retrieved item {ItemId} from cache", id);
                return cachedItem;
            }

            // If not in cache, get from DB
            var item = await _context.Items
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Item not found");

            var itemDto = new ItemDto(
                item.Id,
                item.Name,
                (decimal)item.Price,
                item.ImageUrl,
                item.CategoryId,
                item.Category?.Name ?? "",
                item.SerialNumber,
                item.Quantity,
                item.RowVersion
            );

            // Caching
            await _redisService.SetAsync(cacheKey, itemDto, TimeSpan.FromMinutes(CacheExpiryMinutes));
            _logger.LogDebug("Cached item {ItemId}", id);

            return itemDto;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            const string cacheKey = "categories:all";

            var cachedCategories = await _redisService.GetAsync<List<Category>>(cacheKey);
            if (cachedCategories != null)
            {
                _logger.LogDebug("Retrieved categories from cache");
                return cachedCategories;
            }

            var categories = await _context.Categories.AsNoTracking().ToListAsync();

            // Caching categories
            await _redisService.SetAsync(cacheKey, categories, TimeSpan.FromHours(1));
            _logger.LogDebug("Cached categories");

            return categories;
        }

        public async Task CreateAsync(CreateItemDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create item in DB
                    var item = new Items
                    {
                        Name = dto.Name,
                        Price = (double)dto.Price,
                        ImageUrl = dto.ImageUrl,
                        CategoryId = dto.CategoryId,
                        SerialNumber = dto.SerialNumber,
                        Quantity = dto.Quantity
                    };

                    _context.Items.Add(item);
                    await _context.SaveChangesAsync();

                    // Update Redis
                    var itemDto = new ItemDto(
                        item.Id,
                        item.Name,
                        (decimal)item.Price,
                        item.ImageUrl,
                        item.CategoryId,
                        "", 
                        item.SerialNumber,
                        item.Quantity,
                        item.RowVersion
                    );

                    // Update item:list
                    var productIds = await _redisService.GetAsync<string[]>("item:list") ?? Array.Empty<string>();
                    var updatedProductIds = productIds.Append(item.Id.ToString()).ToArray();
                    var tasks = new List<Task>
                        {
                            _redisService.SetAsync($"item:{item.Id}", item), 
                            _redisService.SetAsync($"item:{item.Id}:dto", itemDto),
                            _redisService.SetAsync($"stock:{item.Id}", item.Quantity),
                            _redisService.SetAsync("item:list", updatedProductIds),
                            _redisService.RemoveAsync("categories:all"),
                            _redisService.RemoveMatchingAsync("item:page:*"), 
                            _redisService.RemoveAsync("admin:items")
                        };

                    await Task.WhenAll(tasks);
                    await transaction.CommitAsync();
                    _logger.LogInformation("Created item {ItemId} and updated cache", item.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating item");
                    throw;
                }
            });
        }

        public async Task UpdateAsync(UpdateItemDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update in DB
                    var existing = await _context.Items
                        .Include(i => i.Category)
                        .FirstOrDefaultAsync(i => i.Id == dto.Id)
                        ?? throw new KeyNotFoundException("Item not found");

                    //// Handle concurrency
                    //if (!dto.RowVersion.SequenceEqual(existing.RowVersion))
                    //{
                    //    throw new DbUpdateConcurrencyException("Item was modified by another user.");
                    //}

                    existing.Name = dto.Name;
                    existing.Price = (double)dto.Price;
                    existing.ImageUrl = dto.ImageUrl;
                    existing.CategoryId = dto.CategoryId;
                    existing.SerialNumber = dto.SerialNumber;
                    existing.Quantity = dto.Quantity;

                    await _context.SaveChangesAsync();

                    // Update Redis
                    var updatedItemDto = new ItemDto(
                        existing.Id,
                        existing.Name,
                        (decimal)existing.Price,
                        existing.ImageUrl,
                        existing.CategoryId,
                        existing.Category?.Name ?? "",
                        existing.SerialNumber,
                        existing.Quantity,
                        existing.RowVersion
                    );

                    var tasks = new List<Task>
                        {
                            _redisService.SetAsync($"item:{existing.Id}", existing), 
                            _redisService.SetAsync($"item:{existing.Id}:dto", updatedItemDto), 
                            _redisService.SetAsync($"stock:{existing.Id}", existing.Quantity),
                            _redisService.RemoveMatchingAsync("item:page:*"),
                            _redisService.RemoveAsync("admin:items")
                        };

                    await Task.WhenAll(tasks);
                    await transaction.CommitAsync();
                    _logger.LogInformation("Updated item {ItemId} and cache", existing.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating item {ItemId}", dto.Id);
                    throw;
                }
            });
        }

        public async Task DeleteAsync(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Delete from DB
                    var existing = await _context.Items.FindAsync(id)
                        ?? throw new KeyNotFoundException("Item not found");

                    _context.Items.Remove(existing);
                    await _context.SaveChangesAsync();

                    // Update item:list
                    var productIds = await _redisService.GetAsync<string[]>("item:list") ?? Array.Empty<string>();
                    var updatedProductIds = productIds.Where(pid => pid != id.ToString()).ToArray();

                    // Update Redis
                    var tasks = new List<Task>
                        {
                            _redisService.RemoveAsync($"item:{id}"),
                            _redisService.RemoveAsync($"item:{id}:dto"),
                            _redisService.RemoveAsync($"stock:{id}"),
                            _redisService.SetAsync("item:list", updatedProductIds),
                            _redisService.RemoveAsync("categories:all"),
                            _redisService.RemoveMatchingAsync("item:page:*"),
                            _redisService.RemoveAsync("admin:items")
                        };

                    await Task.WhenAll(tasks);
                    await transaction.CommitAsync();
                    _logger.LogInformation("Deleted item {ItemId} and updated cache", id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting item {ItemId}", id);
                    throw;
                }
            });
        }

        public async Task<bool> DecreaseStockAsync(int itemId, int quantity)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                // Redis
                var stockKey = $"stock:{itemId}";
                var success = await _redisService.AtomicDecrementAsync(stockKey, quantity);

                if (!success)
                {
                    _logger.LogWarning("Stock decrement failed in Redis for item {ItemId}", itemId);
                    return false;
                }

                // DB
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var item = await _context.Items.FindAsync(itemId);
                    if (item == null || item.Quantity < quantity)
                    {
                        // Rollback Redis
                        await _redisService.AtomicDecrementAsync(stockKey, -quantity);
                        _logger.LogWarning("Stock rollback for item {ItemId}", itemId);
                        return false;
                    }

                    item.Quantity -= quantity;
                    await _context.SaveChangesAsync();

                    // Update Redis
                    var updatedItemDto = new ItemDto(
                        item.Id,
                        item.Name,
                        (decimal)item.Price,
                        item.ImageUrl,
                        item.CategoryId,
                        "", 
                        item.SerialNumber,
                        item.Quantity,
                        item.RowVersion
                    );

                    var tasks = new List<Task>
                        {
                            _redisService.SetAsync($"item:{itemId}", item),
                            _redisService.SetAsync($"item:{itemId}:dto", updatedItemDto),
                            _redisService.RemoveMatchingAsync("item:page:*")
                        };

                    await Task.WhenAll(tasks);
                    await transaction.CommitAsync();
                    _logger.LogInformation("Decreased stock for item {ItemId} by {Quantity}", itemId, quantity);
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback Redis
                    await _redisService.AtomicDecrementAsync(stockKey, -quantity);
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error decreasing stock for item {ItemId}", itemId);
                    throw;
                }
            });
        }
    }
}