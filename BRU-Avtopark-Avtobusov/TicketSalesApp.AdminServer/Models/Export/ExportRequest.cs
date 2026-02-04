using System.ComponentModel.DataAnnotations;

namespace TicketSalesApp.AdminServer.Models.Export
{
    public class ExportRequest
    {
        [Required]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public ExportFormat Format { get; set; }

        public Dictionary<string, object>? Filters { get; set; }

        public string[]? SelectedFields { get; set; }

        public int? MaxRecords { get; set; }

        public bool IncludeHeaders { get; set; } = true;

        public string? FileName { get; set; }

        public Guid RequestedBy { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ExportFormat
    {
        CSV,
        Excel,
        JSON
    }

    public class ExportStatus
    {
        public string JobId { get; set; } = string.Empty;
        public ExportState State { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public double ProgressPercentage => TotalRecords > 0 ? (double)ProcessedRecords / TotalRecords * 100 : 0;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
    }

    public enum ExportState
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public class ExportDownload
    {
        public string DownloadUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    public class ExportFormatInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public bool SupportsStreaming { get; set; }
        public int MaxRecords { get; set; }
    }
}