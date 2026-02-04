#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document for audit logging
    /// </summary>
    [BsonCollection("auditLogs")]
    public class AuditLogDocument : BaseDocument
    {
        [BsonElement("auditId")]
        public Guid AuditId { get; set; }
        
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [BsonElement("userId")]
        public long? UserId { get; set; }
        
        [BsonElement("userName")]
        public string? UserName { get; set; }
        
        [BsonElement("action")]
        public string Action { get; set; } = string.Empty;
        
        [BsonElement("entityType")]
        public string? EntityType { get; set; }
        
        [BsonElement("entityId")]
        public string? EntityId { get; set; }
        
        [BsonElement("entityName")]
        public string? EntityName { get; set; }
        
        [BsonElement("oldValues")]
        public Dictionary<string, object>? OldValues { get; set; }
        
        [BsonElement("newValues")]
        public Dictionary<string, object>? NewValues { get; set; }
        
        [BsonElement("changes")]
        public List<FieldChange>? Changes { get; set; }
        
        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }
        
        [BsonElement("userAgent")]
        public string? UserAgent { get; set; }
        
        [BsonElement("sessionId")]
        public string? SessionId { get; set; }
        
        [BsonElement("correlationId")]
        public string? CorrelationId { get; set; }
        
        [BsonElement("requestPath")]
        public string? RequestPath { get; set; }
        
        [BsonElement("requestMethod")]
        public string? RequestMethod { get; set; }
        
        [BsonElement("success")]
        public bool Success { get; set; } = true;
        
        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }
        
        [BsonElement("duration")]
        public long? Duration { get; set; }
        
        [BsonElement("additionalData")]
        public Dictionary<string, object>? AdditionalData { get; set; }
        
        [BsonElement("tags")]
        public List<string>? Tags { get; set; }
        
        [BsonElement("severity")]
        public string Severity { get; set; } = "Information";
        
        [BsonElement("category")]
        public string? Category { get; set; }
    }
    
    public class FieldChange
    {
        [BsonElement("fieldName")]
        public string FieldName { get; set; } = string.Empty;
        
        [BsonElement("oldValue")]
        public object? OldValue { get; set; }
        
        [BsonElement("newValue")]
        public object? NewValue { get; set; }
        
        [BsonElement("dataType")]
        public string? DataType { get; set; }
    }
}
#endif