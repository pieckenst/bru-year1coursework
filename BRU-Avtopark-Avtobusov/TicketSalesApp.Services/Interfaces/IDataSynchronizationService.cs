using TicketSalesApp.Core.Models;

namespace TicketSalesApp.Services.Interfaces
{
    /// <summary>
    /// Service for synchronizing data between SQL and MongoDB databases
    /// </summary>
    public interface IDataSynchronizationService
    {
        /// <summary>
        /// Synchronize all data from SQL to MongoDB
        /// </summary>
        Task SynchronizeAllAsync();

        /// <summary>
        /// Synchronize specific entity type from SQL to MongoDB
        /// </summary>
        Task SynchronizeEntityAsync<T>() where T : class;

        /// <summary>
        /// Synchronize a specific entity instance from SQL to MongoDB
        /// </summary>
        Task SynchronizeEntityAsync<T>(T entity) where T : class;

        /// <summary>
        /// Get synchronization status and statistics
        /// </summary>
        Task<SynchronizationStatus> GetSynchronizationStatusAsync();

        /// <summary>
        /// Enable or disable automatic synchronization
        /// </summary>
        Task SetAutoSyncAsync(bool enabled);

        /// <summary>
        /// Check if automatic synchronization is enabled
        /// </summary>
        bool IsAutoSyncEnabled { get; }
    }

    /// <summary>
    /// Synchronization status information
    /// </summary>
    public class SynchronizationStatus
    {
        public bool IsEnabled { get; set; }
        public DateTime LastSyncTime { get; set; }
        public Dictionary<string, int> EntityCounts { get; set; } = new();
        public Dictionary<string, DateTime> LastEntitySync { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan TotalSyncTime { get; set; }
    }
}