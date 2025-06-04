using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApplication1.DataTransferObjects;

namespace WebApplication1.Services
{
    public class RedisService
    {
        private readonly IDatabase _redisDb;
        private readonly ILogger<RedisService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _redisDb = redis.GetDatabase();
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                // Prevent serialization cycles
                ReferenceHandler = ReferenceHandler.IgnoreCycles, 
                PropertyNameCaseInsensitive = true, 
                WriteIndented = false
            };
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                await _redisDb.StringSetAsync(key, serializedValue, expiry);
                _logger.LogDebug("Set key {Key} in Redis", key);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize value for key {Key}", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting key {Key} in Redis", key);
                throw;
            }
        }

        public async Task<T?> GetAsync<T>(string key, ItemQueryParameters? parms = null)
        {
            try
            {
                var value = await _redisDb.StringGetAsync(key);
                if (value.IsNullOrEmpty)
                {
                    _logger.LogDebug("Cache miss for key {Key}", key);
                    return default;
                }

                _logger.LogDebug("Cache hit for key {Key}, raw value: {Value}", key, value);
                var result = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize value for key {Key}", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key {Key} from Redis", key);
                return default;
            }
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            try
            {
                var exists = await _redisDb.KeyExistsAsync(key);
                _logger.LogDebug("Checked existence of key {Key}: {Exists}", key, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of key {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _redisDb.KeyDeleteAsync(key);
                _logger.LogDebug("Removed key {Key} from Redis", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing key {Key} from Redis", key);
                throw;
            }
        }

        public async Task<bool> AtomicDecrementAsync(string key, long value = 1)
        {
            try
            {
                var result = await _redisDb.StringDecrementAsync(key, value);
                var success = result >= 0;
                _logger.LogDebug("Decremented key {Key} by {Value}, new value: {Result}, success: {Success}", key, value, result, success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrementing key {Key} by {Value}", key, value);
                return false;
            }
        }

        public async Task RemoveMatchingAsync(string pattern)
        {
            try
            {
                var endpoints = _redisDb.Multiplexer.GetEndPoints();
                var server = _redisDb.Multiplexer.GetServer(endpoints.First());
                var keys = server.Keys(database: _redisDb.Database, pattern: pattern).ToArray();

                if (keys.Any())
                {
                    await _redisDb.KeyDeleteAsync(keys);
                    _logger.LogDebug("Removed {Count} keys matching pattern {Pattern} from Redis", keys.Length, pattern);
                }
                else
                {
                    _logger.LogDebug("No keys found matching pattern {Pattern}", pattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing keys matching pattern {Pattern} from Redis", pattern);
                throw;
            }
        }
    }
}