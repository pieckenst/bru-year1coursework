using Microsoft.Extensions.Logging;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Service for handling cache invalidation based on data changes
    /// </summary>
    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly IResponseCacheService _responseCacheService;
        private readonly ILogger<CacheInvalidationService> _logger;

        public CacheInvalidationService(
            IResponseCacheService responseCacheService,
            ILogger<CacheInvalidationService> logger)
        {
            _responseCacheService = responseCacheService;
            _logger = logger;
        }

        public async Task InvalidateBusCacheAsync(long? busId = null)
        {
            try
            {
                var tags = new List<string> { "buses", "bus-list" };
                
                if (busId.HasValue)
                {
                    tags.Add($"bus:{busId.Value}");
                    
                    // Also invalidate specific entity cache
                    var entityKey = _responseCacheService.GenerateEntityKey("bus", busId.Value);
                    await _responseCacheService.RemoveAsync(entityKey);
                }
                
                // Invalidate related caches
                tags.AddRange(new[] { "routes", "route-list" }); // Routes depend on buses
                
                await _responseCacheService.InvalidateTagsAsync(tags.ToArray());
                _logger.LogInformation("Invalidated bus cache for busId: {BusId}", busId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating bus cache for busId: {BusId}", busId);
            }
        }

        public async Task InvalidateRouteCacheAsync(long? routeId = null)
        {
            try
            {
                var tags = new List<string> { "routes", "route-list" };
                
                if (routeId.HasValue)
                {
                    tags.Add($"route:{routeId.Value}");
                    
                    // Also invalidate specific entity cache
                    var entityKey = _responseCacheService.GenerateEntityKey("route", routeId.Value);
                    await _responseCacheService.RemoveAsync(entityKey);
                }
                
                // Invalidate related caches
                tags.AddRange(new[] { "tickets", "ticket-list", "sales", "sales-list" }); // Sales depend on routes
                
                await _responseCacheService.InvalidateTagsAsync(tags.ToArray());
                _logger.LogInformation("Invalidated route cache for routeId: {RouteId}", routeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating route cache for routeId: {RouteId}", routeId);
            }
        }

        public async Task InvalidateUserCacheAsync(long? userId = null)
        {
            try
            {
                var tags = new List<string> { "users", "user-list" };
                
                if (userId.HasValue)
                {
                    tags.Add($"user:{userId.Value}");
                    
                    // Also invalidate specific entity cache
                    var entityKey = _responseCacheService.GenerateEntityKey("user", userId.Value);
                    await _responseCacheService.RemoveAsync(entityKey);
                    
                    // Invalidate user-specific caches
                    await InvalidateUserSpecificCacheAsync(userId.Value);
                }
                
                // Invalidate related caches
                tags.AddRange(new[] { "roles", "permissions" }); // User changes might affect role assignments
                
                await _responseCacheService.InvalidateTagsAsync(tags.ToArray());
                _logger.LogInformation("Invalidated user cache for userId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating user cache for userId: {UserId}", userId);
            }
        }

        public async Task InvalidateTicketSaleCacheAsync(long? ticketSaleId = null)
        {
            try
            {
                var tags = new List<string> { "tickets", "ticket-list", "sales", "sales-list" };
                
                if (ticketSaleId.HasValue)
                {
                    tags.Add($"ticket:{ticketSaleId.Value}");
                    tags.Add($"sale:{ticketSaleId.Value}");
                    
                    // Also invalidate specific entity cache
                    var entityKey = _responseCacheService.GenerateEntityKey("ticket", ticketSaleId.Value);
                    await _responseCacheService.RemoveAsync(entityKey);
                }
                
                // Invalidate related caches
                tags.AddRange(new[] { "reports", "statistics" }); // Reports depend on sales data
                
                await _responseCacheService.InvalidateTagsAsync(tags.ToArray());
                _logger.LogInformation("Invalidated ticket sale cache for ticketSaleId: {TicketSaleId}", ticketSaleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating ticket sale cache for ticketSaleId: {TicketSaleId}", ticketSaleId);
            }
        }

        public async Task InvalidateEmployeeCacheAsync(long? employeeId = null)
        {
            try
            {
                var tags = new List<string> { "employees", "employee-list" };
                
                if (employeeId.HasValue)
                {
                    tags.Add($"employee:{employeeId.Value}");
                    
                    // Also invalidate specific entity cache
                    var entityKey = _responseCacheService.GenerateEntityKey("employee", employeeId.Value);
                    await _responseCacheService.RemoveAsync(entityKey);
                }
                
                await _responseCacheService.InvalidateTagsAsync(tags.ToArray());
                _logger.LogInformation("Invalidated employee cache for employeeId: {EmployeeId}", employeeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating employee cache for employeeId: {EmployeeId}", employeeId);
            }
        }

        public async Task InvalidateRoleCacheAsync(long? roleId = null, long? userId = null)
        {
            try
            {
                var tags = new List<string> { "roles", "role-list", "permissions", "user-roles" };
                
                if (roleId.HasValue)
                {
                    tags.Add($"role:{roleId.Value}");
                    
                    // Also invalidate specific entity cache
                    var entityKey = _responseCacheService.GenerateEntityKey("role", roleId.Value);
                    await _responseCacheService.RemoveAsync(entityKey);
                }
                
                if (userId.HasValue)
                {
                    tags.Add($"user:{userId.Value}");
                    await InvalidateUserSpecificCacheAsync(userId.Value);
                }
                
                // Invalidate related caches
                tags.AddRange(new[] { "users", "user-list" }); // User data includes role information
                
                await _responseCacheService.InvalidateTagsAsync(tags.ToArray());
                _logger.LogInformation("Invalidated role cache for roleId: {RoleId}, userId: {UserId}", roleId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating role cache for roleId: {RoleId}, userId: {UserId}", roleId, userId);
            }
        }

        public async Task InvalidateEntityTypeAsync(string entityType)
        {
            try
            {
                var normalizedEntityType = entityType.ToLowerInvariant();
                var tags = new[] { normalizedEntityType, $"{normalizedEntityType}-list" };
                
                await _responseCacheService.InvalidateTagsAsync(tags);
                _logger.LogInformation("Invalidated all cache entries for entity type: {EntityType}", entityType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for entity type: {EntityType}", entityType);
            }
        }

        public async Task InvalidateUserSpecificCacheAsync(long userId)
        {
            try
            {
                // Invalidate user-specific cache patterns
                var userKeys = new[]
                {
                    _responseCacheService.GenerateUserKey(userId, "profile"),
                    _responseCacheService.GenerateUserKey(userId, "permissions"),
                    _responseCacheService.GenerateUserKey(userId, "roles"),
                    _responseCacheService.GenerateUserKey(userId, "preferences"),
                    _responseCacheService.GenerateUserKey(userId, "dashboard")
                };
                
                var tasks = userKeys.Select(key => _responseCacheService.RemoveAsync(key));
                await Task.WhenAll(tasks);
                
                _logger.LogInformation("Invalidated user-specific cache for userId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating user-specific cache for userId: {UserId}", userId);
            }
        }

        public async Task InvalidateAllCacheAsync()
        {
            try
            {
                // This is a nuclear option - use with caution
                var allTags = new[]
                {
                    "buses", "bus-list",
                    "routes", "route-list", 
                    "users", "user-list",
                    "tickets", "ticket-list",
                    "sales", "sales-list",
                    "employees", "employee-list",
                    "roles", "role-list",
                    "permissions", "user-roles",
                    "reports", "statistics"
                };
                
                await _responseCacheService.InvalidateTagsAsync(allTags);
                _logger.LogWarning("Invalidated ALL cache entries - this should be used sparingly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating all cache entries");
            }
        }

        public async Task InvalidateTagAsync(string tag)
        {
            try
            {
                await _responseCacheService.InvalidateTagsAsync(new[] { tag });
                _logger.LogInformation("Invalidated cache entries for tag: {Tag}", tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for tag: {Tag}", tag);
            }
        }
    }
}