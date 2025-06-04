using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

public class RedisToDbSynchronizer
{
    private readonly Web1Context _dbContext;
    private readonly RedisService _redisService;
    private readonly ILogger<RedisToDbSynchronizer> _logger;

    public RedisToDbSynchronizer(
        Web1Context dbContext,
        RedisService redisService,
        ILogger<RedisToDbSynchronizer> logger)
    {
        _dbContext = dbContext;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task SyncAllDataAsync()
    {
        _logger.LogInformation("Starting Redis to DB synchronization");

        await SyncProductsAsync();
        await SyncStockAsync();

        _logger.LogInformation("Completed Redis to DB synchronization");
    }

    private async Task SyncProductsAsync()
    {
        var productIds = await _redisService.GetAsync<string[]>("item:list");
        if (productIds == null) return;

        foreach (var idStr in productIds)
        {
            if (int.TryParse(idStr, out var productId))
            {
                var redisProduct = await _redisService.GetAsync<Items>($"item:{productId}");
                if (redisProduct != null)
                {
                    var dbProduct = await _dbContext.Items.FindAsync(productId);
                    if (dbProduct != null)
                    {
                        dbProduct.Name = redisProduct.Name;
                        dbProduct.Price = redisProduct.Price;
                        //dbProduct.Description = redisProduct.Description;

                        _dbContext.Items.Update(dbProduct);
                    }
                }
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SyncStockAsync()
    {
        var productIds = await _redisService.GetAsync<string[]>("item:list");
        if (productIds == null) return;

        foreach (var idStr in productIds)
        {
            if (int.TryParse(idStr, out var productId))
            {
                var redisStock = await _redisService.GetAsync<int?>($"stock:{productId}");
                if (redisStock.HasValue)
                {
                    var dbProduct = await _dbContext.Items.FindAsync(productId);
                    if (dbProduct != null)
                    {
                        dbProduct.Quantity = redisStock.Value;
                        _dbContext.Items.Update(dbProduct);
                    }
                }
            }
        }
        await _dbContext.SaveChangesAsync();
    }
}