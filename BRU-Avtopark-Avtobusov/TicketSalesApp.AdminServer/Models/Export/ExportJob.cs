namespace TicketSalesApp.AdminServer.Models.Export
{
    public class ExportJob
    {
        public string JobId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
        public string[]? SelectedFields { get; set; }
        public int? MaxRecords { get; set; }
        public bool IncludeHeaders { get; set; } = true;
        public string? FileName { get; set; }
        public Guid RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public ExportState State { get; set; } = ExportState.Queued;
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FilePath { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}