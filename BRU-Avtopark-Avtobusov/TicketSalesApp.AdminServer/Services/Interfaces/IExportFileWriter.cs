using TicketSalesApp.AdminServer.Models.Export;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    public interface IExportFileWriter
    {
        /// <summary>
        /// Writes data to a file in the specified format
        /// </summary>
        Task<string> WriteFileAsync(
            IAsyncEnumerable<IEnumerable<object>> dataBatches,
            ExportFormat format,
            string filePath,
            string[]? fieldNames = null,
            bool includeHeaders = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the content type for the specified format
        /// </summary>
        string GetContentType(ExportFormat format);

        /// <summary>
        /// Gets the file extension for the specified format
        /// </summary>
        string GetFileExtension(ExportFormat format);

        /// <summary>
        /// Checks if the format supports streaming
        /// </summary>
        bool SupportsStreaming(ExportFormat format);
    }
}