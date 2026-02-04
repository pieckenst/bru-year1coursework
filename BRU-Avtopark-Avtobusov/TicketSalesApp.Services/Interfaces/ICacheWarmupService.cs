namespace TicketSalesApp.Services.Interfaces
{
    /// <summary>
    /// Service for warming up cache with frequently accessed data
    /// </summary>
    public interface ICacheWarmupService
    {
        /// <summary>
        /// Warm up all frequently accessed data
        /// </summary>
        Task WarmupAllAsync();
        
        /// <summary>
        /// Warm up bus data
        /// </summary>
        Task WarmupBusDataAsync();
        
        /// <summary>
        /// Warm up route data
        /// </summary>
        Task WarmupRouteDataAsync();
        
        /// <summary>
        /// Warm up user data
        /// </summary>
        Task WarmupUserDataAsync();
        
        /// <summary>
        /// Warm up role and permission data
        /// </summary>
        Task WarmupRoleDataAsync();
        
        /// <summary>
        /// Check if cache warmup is needed
        /// </summary>
        Task<bool> IsWarmupNeededAsync();
    }
}