namespace TicketSalesApp.Services.Interfaces
{
    /// <summary>
    /// Service for handling cache invalidation based on data changes
    /// </summary>
    public interface ICacheInvalidationService
    {
        /// <summary>
        /// Invalidate cache when a bus is created, updated, or deleted
        /// </summary>
        Task InvalidateBusCacheAsync(long? busId = null);
        
        /// <summary>
        /// Invalidate cache when a route is created, updated, or deleted
        /// </summary>
        Task InvalidateRouteCacheAsync(long? routeId = null);
        
        /// <summary>
        /// Invalidate cache when a user is created, updated, or deleted
        /// </summary>
        Task InvalidateUserCacheAsync(long? userId = null);
        
        /// <summary>
        /// Invalidate cache when a ticket sale is created, updated, or deleted
        /// </summary>
        Task InvalidateTicketSaleCacheAsync(long? ticketSaleId = null);
        
        /// <summary>
        /// Invalidate cache when an employee is created, updated, or deleted
        /// </summary>
        Task InvalidateEmployeeCacheAsync(long? employeeId = null);
        
        /// <summary>
        /// Invalidate cache when roles or permissions change
        /// </summary>
        Task InvalidateRoleCacheAsync(long? roleId = null, long? userId = null);
        
        /// <summary>
        /// Invalidate all cache entries for a specific entity type
        /// </summary>
        Task InvalidateEntityTypeAsync(string entityType);
        
        /// <summary>
        /// Invalidate user-specific cache entries
        /// </summary>
        Task InvalidateUserSpecificCacheAsync(long userId);
        
        /// <summary>
        /// Invalidate all cache entries (use with caution)
        /// </summary>
        Task InvalidateAllCacheAsync();
        
        /// <summary>
        /// Invalidate cache entries by tag
        /// </summary>
        Task InvalidateTagAsync(string tag);
    }
}