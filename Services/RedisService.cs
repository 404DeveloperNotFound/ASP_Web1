using StackExchange.Redis;
using System.Text.Json;

public class RedisService
{
    private readonly IDatabase _redisDb;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _redisDb.StringSetAsync(key, serializedValue, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _redisDb.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _redisDb.KeyExistsAsync(key);
    }

    public async Task RemoveAsync(string key)
    {
        await _redisDb.KeyDeleteAsync(key);
    }

    public async Task<bool> AtomicDecrementAsync(string key, long value = 1)
    {
        var result = await _redisDb.StringDecrementAsync(key, value);
        return result >= 0;
    }
}