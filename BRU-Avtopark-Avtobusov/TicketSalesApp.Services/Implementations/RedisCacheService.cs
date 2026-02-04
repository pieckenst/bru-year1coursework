using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Redis-based cache service implementation
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisCacheService> logger)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _database = connectionMultiplexer.GetDatabase();
            _logger = logger;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        // Basic cache operations
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                if (!value.HasValue)
                    return default;

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache key {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                await _database.StringSetAsync(key, serializedValue, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key {Key}", key);
            }
        }

        public async Task<bool> RemoveAsync(string key)
        {
            try
            {
                return await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key {Key}", key);
                return false;
            }
        }

        public async Task<long> RemovePatternAsync(string pattern)
        {
            try
            {
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern).ToArray();
                
                if (keys.Length == 0)
                    return 0;

                return await _database.KeyDeleteAsync(keys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache keys with pattern {Pattern}", pattern);
                return 0;
            }
        }

        // Advanced cache operations
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cachedValue = await GetAsync<T>(key);
            if (cachedValue != null)
                return cachedValue;

            var value = await factory();
            await SetAsync(key, value, expiration);
            return value;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if cache key exists {Key}", key);
                return false;
            }
        }

        public async Task<TimeSpan?> GetTtlAsync(string key)
        {
            try
            {
                return await _database.KeyTimeToLiveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TTL for cache key {Key}", key);
                return null;
            }
        }

        public async Task<bool> ExpireAsync(string key, TimeSpan expiration)
        {
            try
            {
                return await _database.KeyExpireAsync(key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting expiration for cache key {Key}", key);
                return false;
            }
        }

        // Bulk operations
        public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys)
        {
            try
            {
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                var values = await _database.StringGetAsync(redisKeys);
                
                var result = new Dictionary<string, T?>();
                for (int i = 0; i < redisKeys.Length; i++)
                {
                    var key = redisKeys[i].ToString();
                    var value = values[i];
                    
                    result[key] = value.HasValue 
                        ? JsonSerializer.Deserialize<T>(value!, _jsonOptions)
                        : default;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple cache keys");
                return new Dictionary<string, T?>();
            }
        }

        public async Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null)
        {
            try
            {
                var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting multiple cache keys");
            }
        }

        public async Task<long> RemoveManyAsync(IEnumerable<string> keys)
        {
            try
            {
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                return await _database.KeyDeleteAsync(redisKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing multiple cache keys");
                return 0;
            }
        }

        // Hash operations
        public async Task<T?> GetHashAsync<T>(string key, string field)
        {
            try
            {
                var value = await _database.HashGetAsync(key, field);
                if (!value.HasValue)
                    return default;

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hash field {Field} from key {Key}", field, key);
                return default;
            }
        }

        public async Task SetHashAsync<T>(string key, string field, T value)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                await _database.HashSetAsync(key, field, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting hash field {Field} in key {Key}", field, key);
            }
        }

        public async Task<Dictionary<string, T?>> GetHashAllAsync<T>(string key)
        {
            try
            {
                var hash = await _database.HashGetAllAsync(key);
                var result = new Dictionary<string, T?>();
                
                foreach (var item in hash)
                {
                    var field = item.Name.ToString();
                    var value = item.Value.HasValue 
                        ? JsonSerializer.Deserialize<T>(item.Value!, _jsonOptions)
                        : default;
                    result[field] = value;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all hash fields from key {Key}", key);
                return new Dictionary<string, T?>();
            }
        }

        public async Task SetHashAllAsync<T>(string key, Dictionary<string, T> hash)
        {
            try
            {
                var hashEntries = hash.Select(kvp => new HashEntry(kvp.Key, JsonSerializer.Serialize(kvp.Value, _jsonOptions))).ToArray();
                await _database.HashSetAsync(key, hashEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting all hash fields in key {Key}", key);
            }
        }

        public async Task<bool> RemoveHashAsync(string key, string field)
        {
            try
            {
                return await _database.HashDeleteAsync(key, field);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing hash field {Field} from key {Key}", field, key);
                return false;
            }
        }

        // List operations
        public async Task<long> ListPushAsync<T>(string key, T value)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                return await _database.ListLeftPushAsync(key, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing to list {Key}", key);
                return 0;
            }
        }

        public async Task<T?> ListPopAsync<T>(string key)
        {
            try
            {
                var value = await _database.ListLeftPopAsync(key);
                if (!value.HasValue)
                    return default;

                return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error popping from list {Key}", key);
                return default;
            }
        }

        public async Task<List<T>> ListRangeAsync<T>(string key, int start = 0, int stop = -1)
        {
            try
            {
                var values = await _database.ListRangeAsync(key, start, stop);
                var result = new List<T>();
                
                foreach (var value in values)
                {
                    if (value.HasValue)
                    {
                        var item = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                        if (item != null)
                            result.Add(item);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting list range from {Key}", key);
                return new List<T>();
            }
        }

        public async Task<long> ListLengthAsync(string key)
        {
            try
            {
                return await _database.ListLengthAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting list length for {Key}", key);
                return 0;
            }
        }

        // Set operations
        public async Task<bool> SetAddAsync<T>(string key, T value)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                return await _database.SetAddAsync(key, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to set {Key}", key);
                return false;
            }
        }

        public async Task<bool> SetRemoveAsync<T>(string key, T value)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                return await _database.SetRemoveAsync(key, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from set {Key}", key);
                return false;
            }
        }

        public async Task<List<T>> SetMembersAsync<T>(string key)
        {
            try
            {
                var values = await _database.SetMembersAsync(key);
                var result = new List<T>();
                
                foreach (var value in values)
                {
                    if (value.HasValue)
                    {
                        var item = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                        if (item != null)
                            result.Add(item);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting set members from {Key}", key);
                return new List<T>();
            }
        }

        public async Task<bool> SetContainsAsync<T>(string key, T value)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                return await _database.SetContainsAsync(key, serializedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking set membership in {Key}", key);
                return false;
            }
        }

        public async Task<long> SetLengthAsync(string key)
        {
            try
            {
                return await _database.SetLengthAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting set length for {Key}", key);
                return 0;
            }
        }

        // Cache management
        public async Task FlushAllAsync()
        {
            try
            {
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                await server.FlushAllDatabasesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing all cache");
            }
        }

        public async Task<Dictionary<string, object>> GetCacheInfoAsync()
        {
            try
            {
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var info = await server.InfoAsync();
                
                var result = new Dictionary<string, object>();
                foreach (var section in info)
                {
                    foreach (var item in section)
                    {
                        result[item.Key] = item.Value;
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache info");
                return new Dictionary<string, object> { ["Error"] = ex.Message };
            }
        }

        public async Task<List<string>> GetKeysAsync(string pattern = "*")
        {
            try
            {
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern);
                return await Task.FromResult(keys.Select(k => k.ToString()).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache keys with pattern {Pattern}", pattern);
                return new List<string>();
            }
        }

        // Event-driven cache invalidation
        public async Task InvalidateTagAsync(string tag)
        {
            await RemovePatternAsync($"tag:{tag}:*");
        }

        public async Task InvalidateTagsAsync(IEnumerable<string> tags)
        {
            var tasks = tags.Select(InvalidateTagAsync);
            await Task.WhenAll(tasks);
        }

        public async Task SetWithTagsAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiration = null)
        {
            await SetAsync(key, value, expiration);
            
            // Store tag associations
            foreach (var tag in tags)
            {
                var tagKey = $"tag:{tag}:{key}";
                await SetAsync(tagKey, true, expiration);
            }
        }
    }
}