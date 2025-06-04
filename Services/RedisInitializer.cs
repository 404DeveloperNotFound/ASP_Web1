using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Services;

public class RedisInitializer
{
    private readonly Web1Context _dbContext;
    private readonly RedisService _redisService;

    public RedisInitializer(Web1Context dbContext, RedisService redisService)
    {
        _dbContext = dbContext;
        _redisService = redisService;
    }

    public async Task InitializeDataAsync()
    {
        // Load products from database
        var products = await _dbContext.Items.ToListAsync();

        // Store each product in Redis
        foreach (var product in products)
        {
            await _redisService.SetAsync($"item:{product.Id}", product);
            await _redisService.SetAsync($"stock:{product.Id}", product.Quantity);
        }

        // Create product list index
        var productIds = products.Select(p => p.Id.ToString()).ToArray();
        await _redisService.SetAsync("item:list", productIds);

        Console.WriteLine($"Initialized {products.Count} Items in Redis");
    }
}