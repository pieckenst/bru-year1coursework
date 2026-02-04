using TicketSalesApp.AdminServer.Models.Export;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    public interface IExportService
    {
        /// <summary>
        /// Initiates an export job for the specified entity type and format
        /// </summary>
        /// <param name="request">Export request parameters</param>
        /// <returns>Export job ID for tracking progress</returns>
        Task<string> StartExportAsync(ExportRequest request);

        /// <summary>
        /// Gets the status of an export job
        /// </summary>
        /// <param name="jobId">Export job ID</param>
        /// <returns>Export status information</returns>
        Task<ExportStatus> GetExportStatusAsync(string jobId);

        /// <summary>
        /// Gets the download URL for a completed export
        /// </summary>
        /// <param name="jobId">Export job ID</param>
        /// <returns>Download URL with expiration</returns>
        Task<ExportDownload> GetExportDownloadAsync(string jobId);

        /// <summary>
        /// Cancels an ongoing export job
        /// </summary>
        /// <param name="jobId">Export job ID</param>
        /// <returns>True if successfully cancelled</returns>
        Task<bool> CancelExportAsync(string jobId);

        /// <summary>
        /// Cleans up expired export files
        /// </summary>
        /// <returns>Number of files cleaned up</returns>
        Task<int> CleanupExpiredExportsAsync();

        /// <summary>
        /// Gets available export formats for an entity type
        /// </summary>
        /// <param name="entityType">Entity type to export</param>
        /// <returns>List of supported formats</returns>
        Task<IEnumerable<ExportFormatInfo>> GetSupportedFormatsAsync(string entityType);
    }
}