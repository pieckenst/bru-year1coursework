namespace TicketSalesApp.Services.Interfaces
{
    /// <summary>
    /// Service for caching API responses with automatic invalidation
    /// </summary>
    public interface IResponseCacheService
    {
        /// <summary>
        /// Get cached response or execute factory function and cache result
        /// </summary>
        Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null, params string[] tags);
        
        /// <summary>
        /// Get cached response
        /// </summary>
        Task<T?> GetAsync<T>(string cacheKey);
        
        /// <summary>
        /// Set cached response with tags for invalidation
        /// </summary>
        Task SetAsync<T>(string cacheKey, T value, TimeSpan? expiration = null, params string[] tags);
        
        /// <summary>
        /// Remove cached response
        /// </summary>
        Task RemoveAsync(string cacheKey);
        
        /// <summary>
        /// Invalidate all cache entries with specific tag
        /// </summary>
        Task InvalidateTagAsync(string tag);
        
        /// <summary>
        /// Invalidate all cache entries with any of the specified tags
        /// </summary>
        Task InvalidateTagsAsync(params string[] tags);
        
        /// <summary>
        /// Generate cache key for entity list
        /// </summary>
        string GenerateListKey(string entityType, string? filter = null, int? page = null, int? pageSize = null);
        
        /// <summary>
        /// Generate cache key for single entity
        /// </summary>
        string GenerateEntityKey(string entityType, object id);
        
        /// <summary>
        /// Generate cache key for user-specific data
        /// </summary>
        string GenerateUserKey(long userId, string dataType);
    }
}