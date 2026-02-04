namespace TicketSalesApp.Services.Interfaces
{
    /// <summary>
    /// Cache service interface for distributed caching operations
    /// </summary>
    public interface ICacheService
    {
        // Basic cache operations
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<bool> RemoveAsync(string key);
        Task<long> RemovePatternAsync(string pattern);
        
        // Advanced cache operations
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task<bool> ExistsAsync(string key);
        Task<TimeSpan?> GetTtlAsync(string key);
        Task<bool> ExpireAsync(string key, TimeSpan expiration);
        
        // Bulk operations
        Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys);
        Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null);
        Task<long> RemoveManyAsync(IEnumerable<string> keys);
        
        // Hash operations (for complex objects)
        Task<T?> GetHashAsync<T>(string key, string field);
        Task SetHashAsync<T>(string key, string field, T value);
        Task<Dictionary<string, T?>> GetHashAllAsync<T>(string key);
        Task SetHashAllAsync<T>(string key, Dictionary<string, T> hash);
        Task<bool> RemoveHashAsync(string key, string field);
        
        // List operations
        Task<long> ListPushAsync<T>(string key, T value);
        Task<T?> ListPopAsync<T>(string key);
        Task<List<T>> ListRangeAsync<T>(string key, int start = 0, int stop = -1);
        Task<long> ListLengthAsync(string key);
        
        // Set operations
        Task<bool> SetAddAsync<T>(string key, T value);
        Task<bool> SetRemoveAsync<T>(string key, T value);
        Task<List<T>> SetMembersAsync<T>(string key);
        Task<bool> SetContainsAsync<T>(string key, T value);
        Task<long> SetLengthAsync(string key);
        
        // Cache management
        Task FlushAllAsync();
        Task<Dictionary<string, object>> GetCacheInfoAsync();
        Task<List<string>> GetKeysAsync(string pattern = "*");
        
        // Event-driven cache invalidation
        Task InvalidateTagAsync(string tag);
        Task InvalidateTagsAsync(IEnumerable<string> tags);
        Task SetWithTagsAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiration = null);
    }
}