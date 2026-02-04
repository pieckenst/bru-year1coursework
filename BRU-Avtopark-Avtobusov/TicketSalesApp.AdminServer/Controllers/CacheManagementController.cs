using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Controller for managing cache operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class CacheManagementController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly IResponseCacheService _responseCacheService;
        private readonly ICacheInvalidationService _cacheInvalidationService;
        private readonly ICacheWarmupService _cacheWarmupService;
        private readonly ILogger<CacheManagementController> _logger;

        public CacheManagementController(
            ICacheService cacheService,
            IResponseCacheService responseCacheService,
            ICacheInvalidationService cacheInvalidationService,
            ICacheWarmupService cacheWarmupService,
            ILogger<CacheManagementController> logger)
        {
            _cacheService = cacheService;
            _responseCacheService = responseCacheService;
            _cacheInvalidationService = cacheInvalidationService;
            _cacheWarmupService = cacheWarmupService;
            _logger = logger;
        }

        /// <summary>
        /// Get cache information and statistics
        /// </summary>
        [HttpGet("info")]
        public async Task<IActionResult> GetCacheInfo()
        {
            try
            {
                var cacheInfo = await _cacheService.GetCacheInfoAsync();
                var isWarmupNeeded = await _cacheWarmupService.IsWarmupNeededAsync();

                var response = new
                {
                    CacheInfo = cacheInfo,
                    IsWarmupNeeded = isWarmupNeeded,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache information");
                return StatusCode(500, new { Error = "Failed to get cache information" });
            }
        }

        /// <summary>
        /// Get all cache keys matching a pattern
        /// </summary>
        [HttpGet("keys")]
        public async Task<IActionResult> GetCacheKeys([FromQuery] string pattern = "*")
        {
            try
            {
                var keys = await _cacheService.GetKeysAsync(pattern);
                return Ok(new { Keys = keys, Pattern = pattern, Count = keys.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache keys with pattern {Pattern}", pattern);
                return StatusCode(500, new { Error = "Failed to get cache keys" });
            }
        }

        /// <summary>
        /// Warm up cache with frequently accessed data
        /// </summary>
        [HttpPost("warmup")]
        public async Task<IActionResult> WarmupCache()
        {
            try
            {
                _logger.LogInformation("Cache warmup requested by admin user");
                await _cacheWarmupService.WarmupAllAsync();
                
                return Ok(new { Message = "Cache warmup completed successfully", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warmup");
                return StatusCode(500, new { Error = "Cache warmup failed" });
            }
        }

        /// <summary>
        /// Warm up specific entity type cache
        /// </summary>
        [HttpPost("warmup/{entityType}")]
        public async Task<IActionResult> WarmupEntityCache(string entityType)
        {
            try
            {
                _logger.LogInformation("Cache warmup requested for entity type {EntityType} by admin user", entityType);
                
                switch (entityType.ToLowerInvariant())
                {
                    case "buses":
                        await _cacheWarmupService.WarmupBusDataAsync();
                        break;
                    case "routes":
                        await _cacheWarmupService.WarmupRouteDataAsync();
                        break;
                    case "users":
                        await _cacheWarmupService.WarmupUserDataAsync();
                        break;
                    case "roles":
                        await _cacheWarmupService.WarmupRoleDataAsync();
                        break;
                    default:
                        return BadRequest(new { Error = $"Unknown entity type: {entityType}" });
                }
                
                return Ok(new { Message = $"Cache warmup completed for {entityType}", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warmup for entity type {EntityType}", entityType);
                return StatusCode(500, new { Error = $"Cache warmup failed for {entityType}" });
            }
        }

        /// <summary>
        /// Invalidate cache by tag
        /// </summary>
        [HttpDelete("invalidate/tag/{tag}")]
        public async Task<IActionResult> InvalidateByTag(string tag)
        {
            try
            {
                _logger.LogInformation("Cache invalidation requested for tag {Tag} by admin user", tag);
                await _cacheInvalidationService.InvalidateTagAsync(tag);
                
                return Ok(new { Message = $"Cache invalidated for tag: {tag}", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for tag {Tag}", tag);
                return StatusCode(500, new { Error = $"Cache invalidation failed for tag: {tag}" });
            }
        }

        /// <summary>
        /// Invalidate cache for specific entity type
        /// </summary>
        [HttpDelete("invalidate/entity/{entityType}")]
        public async Task<IActionResult> InvalidateEntityType(string entityType)
        {
            try
            {
                _logger.LogInformation("Cache invalidation requested for entity type {EntityType} by admin user", entityType);
                
                switch (entityType.ToLowerInvariant())
                {
                    case "buses":
                        await _cacheInvalidationService.InvalidateBusCacheAsync();
                        break;
                    case "routes":
                        await _cacheInvalidationService.InvalidateRouteCacheAsync();
                        break;
                    case "users":
                        await _cacheInvalidationService.InvalidateUserCacheAsync();
                        break;
                    case "employees":
                        await _cacheInvalidationService.InvalidateEmployeeCacheAsync();
                        break;
                    case "roles":
                        await _cacheInvalidationService.InvalidateRoleCacheAsync();
                        break;
                    case "tickets":
                        await _cacheInvalidationService.InvalidateTicketSaleCacheAsync();
                        break;
                    default:
                        await _cacheInvalidationService.InvalidateEntityTypeAsync(entityType);
                        break;
                }
                
                return Ok(new { Message = $"Cache invalidated for entity type: {entityType}", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for entity type {EntityType}", entityType);
                return StatusCode(500, new { Error = $"Cache invalidation failed for entity type: {entityType}" });
            }
        }

        /// <summary>
        /// Invalidate specific cache key
        /// </summary>
        [HttpDelete("invalidate/key/{key}")]
        public async Task<IActionResult> InvalidateKey(string key)
        {
            try
            {
                _logger.LogInformation("Cache invalidation requested for key {Key} by admin user", key);
                await _responseCacheService.RemoveAsync(key);
                
                return Ok(new { Message = $"Cache invalidated for key: {key}", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for key {Key}", key);
                return StatusCode(500, new { Error = $"Cache invalidation failed for key: {key}" });
            }
        }

        /// <summary>
        /// Flush all cache (use with extreme caution)
        /// </summary>
        [HttpDelete("flush")]
        public async Task<IActionResult> FlushAllCache()
        {
            try
            {
                _logger.LogWarning("FULL CACHE FLUSH requested by admin user - this will clear ALL cached data");
                await _cacheService.FlushAllAsync();
                
                return Ok(new { Message = "All cache data has been flushed", Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing all cache");
                return StatusCode(500, new { Error = "Cache flush failed" });
            }
        }

        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetCacheStats()
        {
            try
            {
                var allKeys = await _cacheService.GetKeysAsync("*");
                var keysByType = allKeys
                    .GroupBy(key => key.Split(':').FirstOrDefault() ?? "unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                var stats = new
                {
                    TotalKeys = allKeys.Count,
                    KeysByType = keysByType,
                    IsWarmupNeeded = await _cacheWarmupService.IsWarmupNeededAsync(),
                    Timestamp = DateTime.UtcNow
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return StatusCode(500, new { Error = "Failed to get cache statistics" });
            }
        }
    }
}