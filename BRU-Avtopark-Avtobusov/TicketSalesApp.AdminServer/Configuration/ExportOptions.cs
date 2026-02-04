namespace TicketSalesApp.AdminServer.Configuration
{
    public class ExportOptions
    {
        public const string SectionName = "Export";

        /// <summary>
        /// Directory where export files are stored
        /// </summary>
        public string ExportDirectory { get; set; } = "exports";

        /// <summary>
        /// How long export files are kept before cleanup (in hours)
        /// </summary>
        public int FileExpirationHours { get; set; } = 24;

        /// <summary>
        /// Maximum file size for exports (in bytes)
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 MB

        /// <summary>
        /// Default batch size for streaming exports
        /// </summary>
        public int DefaultBatchSize { get; set; } = 1000;

        /// <summary>
        /// Maximum number of concurrent export jobs
        /// </summary>
        public int MaxConcurrentJobs { get; set; } = 5;

        /// <summary>
        /// Base URL for download links
        /// </summary>
        public string? BaseDownloadUrl { get; set; }
    }
}