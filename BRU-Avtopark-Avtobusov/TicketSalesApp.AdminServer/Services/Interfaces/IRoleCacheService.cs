using TicketSalesApp.AdminServer.Authorization;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    /// <summary>
    /// Service for managing role cache invalidation
    /// </summary>
    public interface IRoleCacheService
    {
        /// <summary>
        /// Invalidate cached roles for a specific user
        /// </summary>
        /// <param name="userId">User ID to invalidate cache for</param>
        Task InvalidateUserRolesAsync(long userId);

        /// <summary>
        /// Invalidate cached roles for multiple users
        /// </summary>
        /// <param name="userIds">User IDs to invalidate cache for</param>
        Task InvalidateUserRolesAsync(IEnumerable<long> userIds);

        /// <summary>
        /// Invalidate all cached role data
        /// </summary>
        Task InvalidateAllRoleCacheAsync();

        /// <summary>
        /// Get cached user role information
        /// </summary>
        /// <param name="userId">User ID to get cached info for</param>
        /// <returns>Cached user role info or null if not cached</returns>
        Task<UserRoleInfo?> GetCachedUserRoleInfoAsync(long userId);

        /// <summary>
        /// Preload user role information into cache
        /// </summary>
        /// <param name="userId">User ID to preload</param>
        Task PreloadUserRoleInfoAsync(long userId);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        Task<RoleCacheStatistics> GetCacheStatisticsAsync();
    }

    /// <summary>
    /// Statistics about the role cache
    /// </summary>
    public class RoleCacheStatistics
    {
        public int TotalCachedUsers { get; set; }
        public DateTime? OldestCacheEntry { get; set; }
        public DateTime? NewestCacheEntry { get; set; }
        public TimeSpan CacheExpiration { get; set; }
        public long EstimatedMemoryUsage { get; set; }
    }
}