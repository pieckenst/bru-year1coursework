using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TicketSalesApp.AdminServer.Authorization;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Service for managing role cache invalidation and statistics
    /// </summary>
    public class RoleCacheService : IRoleCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly AppDbContext _context;
        private readonly ILogger<RoleCacheService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
        private readonly ConcurrentDictionary<long, DateTime> _cacheTracker = new();

        public RoleCacheService(
            IMemoryCache cache,
            AppDbContext context,
            ILogger<RoleCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task InvalidateUserRolesAsync(long userId)
        {
            var cacheKey = $"user_roles_{userId}";
            _cache.Remove(cacheKey);
            _cacheTracker.TryRemove(userId, out _);
            
            _logger.LogInformation("Invalidated role cache for user {UserId}", userId);
            return Task.CompletedTask;
        }

        public Task InvalidateUserRolesAsync(IEnumerable<long> userIds)
        {
            var invalidatedCount = 0;
            foreach (var userId in userIds)
            {
                var cacheKey = $"user_roles_{userId}";
                _cache.Remove(cacheKey);
                _cacheTracker.TryRemove(userId, out _);
                invalidatedCount++;
            }
            
            _logger.LogInformation("Invalidated role cache for {Count} users", invalidatedCount);
            return Task.CompletedTask;
        }

        public Task InvalidateAllRoleCacheAsync()
        {
            var userIds = _cacheTracker.Keys.ToList();
            foreach (var userId in userIds)
            {
                var cacheKey = $"user_roles_{userId}";
                _cache.Remove(cacheKey);
            }
            
            _cacheTracker.Clear();
            _logger.LogInformation("Invalidated all role cache entries ({Count} users)", userIds.Count);
            return Task.CompletedTask;
        }

        public Task<UserRoleInfo?> GetCachedUserRoleInfoAsync(long userId)
        {
            var cacheKey = $"user_roles_{userId}";
            _cache.TryGetValue(cacheKey, out UserRoleInfo? userRoleInfo);
            return Task.FromResult(userRoleInfo);
        }

        public async Task PreloadUserRoleInfoAsync(long userId)
        {
            try
            {
                var cacheKey = $"user_roles_{userId}";
                
                // Check if already cached
                if (_cache.TryGetValue(cacheKey, out _))
                {
                    return;
                }

                var userRoleInfo = await GetUserRoleInfoFromDatabase(userId);
                if (userRoleInfo != null)
                {
                    _cache.Set(cacheKey, userRoleInfo, _cacheExpiration);
                    _cacheTracker[userId] = DateTime.UtcNow;
                    
                    _logger.LogDebug("Preloaded role cache for user {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preloading role cache for user {UserId}", userId);
            }
        }

        public Task<RoleCacheStatistics> GetCacheStatisticsAsync()
        {
            var cachedUsers = _cacheTracker.Keys.ToList();
            var cacheEntries = _cacheTracker.Values.ToList();
            
            var statistics = new RoleCacheStatistics
            {
                TotalCachedUsers = cachedUsers.Count,
                OldestCacheEntry = cacheEntries.Any() ? cacheEntries.Min() : null,
                NewestCacheEntry = cacheEntries.Any() ? cacheEntries.Max() : null,
                CacheExpiration = _cacheExpiration,
                EstimatedMemoryUsage = EstimateMemoryUsage(cachedUsers.Count)
            };

            return Task.FromResult(statistics);
        }

        private async Task<UserRoleInfo?> GetUserRoleInfoFromDatabase(long userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles!)
                        .ThenInclude(ur => ur.Role!)
                            .ThenInclude(r => r.RolePermissions!)
                                .ThenInclude(rp => rp.Permission!)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return null;
                }

                var modernRoles = user.UserRoles?
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!)
                    .ToList() ?? new List<Roles>();

                var permissions = modernRoles
                    .SelectMany(r => r.RolePermissions ?? new List<RolePermission>())
                    .Where(rp => rp.Permission != null)
                    .Select(rp => rp.Permission!)
                    .Distinct()
                    .ToList();

                return new UserRoleInfo
                {
                    UserId = userId,
                    LegacyRole = user.Role,
                    ModernRoles = modernRoles,
                    Permissions = permissions,
                    IsActive = user.IsActive,
                    CachedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user role info for user {UserId}", userId);
                return null;
            }
        }

        private long EstimateMemoryUsage(int cachedUserCount)
        {
            // Rough estimate: each cached user entry is approximately 2KB
            // This includes the UserRoleInfo object, roles, permissions, etc.
            const long bytesPerUser = 2048;
            return cachedUserCount * bytesPerUser;
        }
    }
}