#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document for export job tracking
    /// </summary>
    [BsonCollection("exportJobs")]
    public class ExportJobDocument : BaseDocument
    {
        [BsonElement("jobId")]
        public Guid JobId { get; set; }
        
        [BsonElement("userId")]
        public long UserId { get; set; }
        
        [BsonElement("entityType")]
        public string EntityType { get; set; } = string.Empty;
        
        [BsonElement("format")]
        public string Format { get; set; } = string.Empty;
        
        [BsonElement("status")]
        public string Status { get; set; } = "Queued";
        
        [BsonElement("progress")]
        public int Progress { get; set; }
        
        [BsonElement("totalRecords")]
        public long? TotalRecords { get; set; }
        
        [BsonElement("processedRecords")]
        public long? ProcessedRecords { get; set; }
        
        [BsonElement("filters")]
        public Dictionary<string, object>? Filters { get; set; }
        
        [BsonElement("columns")]
        public List<string>? Columns { get; set; }
        
        [BsonElement("filePath")]
        public string? FilePath { get; set; }
        
        [BsonElement("fileName")]
        public string? FileName { get; set; }
        
        [BsonElement("fileSizeBytes")]
        public long? FileSizeBytes { get; set; }
        
        [BsonElement("downloadUrl")]
        public string? DownloadUrl { get; set; }
        
        [BsonElement("expiresAt")]
        public DateTime ExpiresAt { get; set; }
        
        [BsonElement("startedAt")]
        public DateTime? StartedAt { get; set; }
        
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; set; }
        
        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }
        
        [BsonElement("errorDetails")]
        public string? ErrorDetails { get; set; }
        
        [BsonElement("retryCount")]
        public int RetryCount { get; set; }
        
        [BsonElement("maxRetries")]
        public int MaxRetries { get; set; } = 3;
        
        [BsonElement("configuration")]
        public ExportConfiguration? Configuration { get; set; }
    }
    
    public class ExportConfiguration
    {
        [BsonElement("includeHeaders")]
        public bool IncludeHeaders { get; set; } = true;
        
        [BsonElement("dateFormat")]
        public string? DateFormat { get; set; }
        
        [BsonElement("numberFormat")]
        public string? NumberFormat { get; set; }
        
        [BsonElement("delimiter")]
        public string? Delimiter { get; set; }
        
        [BsonElement("encoding")]
        public string? Encoding { get; set; }
        
        [BsonElement("compressionType")]
        public string? CompressionType { get; set; }
        
        [BsonElement("maxRecordsPerFile")]
        public int? MaxRecordsPerFile { get; set; }
        
        [BsonElement("splitLargeFiles")]
        public bool SplitLargeFiles { get; set; }
        
        [BsonElement("customProperties")]
        public Dictionary<string, object>? CustomProperties { get; set; }
    }
}
#endif