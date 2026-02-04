using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Response caching service with tag-based invalidation
    /// </summary>
    public class ResponseCacheService : IResponseCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<ResponseCacheService> _logger;
        private readonly TimeSpan _defaultExpiration;
        private readonly Dictionary<string, TimeSpan> _entityExpirations;

        public ResponseCacheService(
            ICacheService cacheService,
            IConfiguration configuration,
            ILogger<ResponseCacheService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
            
            // Load cache configuration
            var cachingSection = configuration.GetSection("Caching");
            _defaultExpiration = TimeSpan.Parse(cachingSection.GetValue<string>("DefaultExpiration") ?? "00:15:00");
            
            // Load entity-specific expiration times
            _entityExpirations = new Dictionary<string, TimeSpan>();
            var policies = cachingSection.GetSection("Policies");
            foreach (var policy in policies.GetChildren())
            {
                if (TimeSpan.TryParse(policy.Value, out var expiration))
                {
                    _entityExpirations[policy.Key.ToLowerInvariant()] = expiration;
                }
            }
        }

        public async Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null, params string[] tags)
        {
            try
            {
                // Try to get from cache first
                var cachedValue = await _cacheService.GetAsync<T>(cacheKey);
                if (cachedValue != null)
                {
                    _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                    return cachedValue;
                }

                // Cache miss - execute factory function
                _logger.LogDebug("Cache miss for key: {CacheKey}, executing factory function", cacheKey);
                var value = await factory();
                
                // Cache the result with tags
                var effectiveExpiration = expiration ?? GetExpirationForKey(cacheKey);
                await SetAsync(cacheKey, value, effectiveExpiration, tags);
                
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSetAsync for key: {CacheKey}", cacheKey);
                
                // If caching fails, still execute the factory function
                return await factory();
            }
        }

        public async Task<T?> GetAsync<T>(string cacheKey)
        {
            try
            {
                return await _cacheService.GetAsync<T>(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache key: {CacheKey}", cacheKey);
                return default;
            }
        }

        public async Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiration = null, params string[] tags)
        {
            try
            {
                var effectiveExpiration = expiration ?? GetExpirationForKey(cacheKey);
                
                if (tags.Length > 0)
                {
                    // Use tagged caching for invalidation support
                    await _cacheService.SetWithTagsAsync(cacheKey, value, tags, effectiveExpiration);
                }
                else
                {
                    // Simple caching without tags
                    await _cacheService.SetAsync(cacheKey, value, effectiveExpiration);
                }
                
                _logger.LogDebug("Cached key: {CacheKey} with expiration: {Expiration} and tags: {Tags}", 
                    cacheKey, effectiveExpiration, string.Join(", ", tags));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {CacheKey}", cacheKey);
            }
        }

        public async Task RemoveAsync(string cacheKey)
        {
            try
            {
                await _cacheService.RemoveAsync(cacheKey);
                _logger.LogDebug("Removed cache key: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {CacheKey}", cacheKey);
            }
        }

        public async Task InvalidateTagAsync(string tag)
        {
            try
            {
                await _cacheService.InvalidateTagAsync(tag);
                _logger.LogInformation("Invalidated cache tag: {Tag}", tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache tag: {Tag}", tag);
            }
        }

        public async Task InvalidateTagsAsync(params string[] tags)
        {
            try
            {
                await _cacheService.InvalidateTagsAsync(tags);
                _logger.LogInformation("Invalidated cache tags: {Tags}", string.Join(", ", tags));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache tags: {Tags}", string.Join(", ", tags));
            }
        }

        public string GenerateListKey(string entityType, string? filter = null, int? page = null, int? pageSize = null)
        {
            var keyBuilder = new StringBuilder($"list:{entityType.ToLowerInvariant()}");
            
            if (!string.IsNullOrEmpty(filter))
            {
                // Hash the filter to keep key length manageable
                var filterHash = ComputeHash(filter);
                keyBuilder.Append($":filter:{filterHash}");
            }
            
            if (page.HasValue)
            {
                keyBuilder.Append($":page:{page.Value}");
            }
            
            if (pageSize.HasValue)
            {
                keyBuilder.Append($":size:{pageSize.Value}");
            }
            
            return keyBuilder.ToString();
        }

        public string GenerateEntityKey(string entityType, object id)
        {
            return $"entity:{entityType.ToLowerInvariant()}:{id}";
        }

        public string GenerateUserKey(long userId, string dataType)
        {
            return $"user:{userId}:{dataType.ToLowerInvariant()}";
        }

        /// <summary>
        /// Get expiration time based on cache key patterns
        /// </summary>
        private TimeSpan GetExpirationForKey(string cacheKey)
        {
            // Extract entity type from cache key
            var parts = cacheKey.Split(':');
            if (parts.Length >= 2)
            {
                var entityType = parts[1].ToLowerInvariant();
                if (_entityExpirations.TryGetValue(entityType, out var expiration))
                {
                    return expiration;
                }
            }
            
            return _defaultExpiration;
        }

        /// <summary>
        /// Compute SHA256 hash of input string
        /// </summary>
        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes)[..16]; // Take first 16 characters
        }
    }
}