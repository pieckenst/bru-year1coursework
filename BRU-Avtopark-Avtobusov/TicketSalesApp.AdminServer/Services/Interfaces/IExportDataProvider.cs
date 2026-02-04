namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    public interface IExportDataProvider
    {
        /// <summary>
        /// Gets the total count of records for the given entity type and filters
        /// </summary>
        Task<int> GetTotalCountAsync(string entityType, Dictionary<string, object>? filters = null);

        /// <summary>
        /// Gets data in batches for streaming export
        /// </summary>
        IAsyncEnumerable<IEnumerable<object>> GetDataBatchesAsync(
            string entityType, 
            Dictionary<string, object>? filters = null,
            string[]? selectedFields = null,
            int batchSize = 1000,
            int? maxRecords = null);

        /// <summary>
        /// Gets all data at once (for smaller datasets)
        /// </summary>
        Task<IEnumerable<object>> GetAllDataAsync(
            string entityType, 
            Dictionary<string, object>? filters = null,
            string[]? selectedFields = null,
            int? maxRecords = null);

        /// <summary>
        /// Gets available fields for an entity type
        /// </summary>
        Task<IEnumerable<string>> GetAvailableFieldsAsync(string entityType);

        /// <summary>
        /// Checks if an entity type is supported for export
        /// </summary>
        bool IsEntityTypeSupported(string entityType);
    }
}