using TicketSalesApp.AdminServer.Models.Export;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    public interface IExportProgressTracker
    {
        /// <summary>
        /// Updates the progress of an export job
        /// </summary>
        Task UpdateProgressAsync(string jobId, int processedRecords, int totalRecords);

        /// <summary>
        /// Marks an export job as started
        /// </summary>
        Task MarkStartedAsync(string jobId, int totalRecords);

        /// <summary>
        /// Marks an export job as completed
        /// </summary>
        Task MarkCompletedAsync(string jobId, string filePath, long fileSizeBytes);

        /// <summary>
        /// Marks an export job as failed
        /// </summary>
        Task MarkFailedAsync(string jobId, string errorMessage);

        /// <summary>
        /// Marks an export job as cancelled
        /// </summary>
        Task MarkCancelledAsync(string jobId);

        /// <summary>
        /// Sends progress notification via WebSocket
        /// </summary>
        Task NotifyProgressAsync(string jobId, ExportStatus status, Guid userId);

        /// <summary>
        /// Gets an export job by ID
        /// </summary>
        Task<ExportJob?> GetJobAsync(string jobId);

        /// <summary>
        /// Saves an export job
        /// </summary>
        Task SaveJobAsync(ExportJob job);
    }
}