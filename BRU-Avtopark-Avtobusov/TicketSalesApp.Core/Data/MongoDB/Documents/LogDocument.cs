#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document for structured logging
    /// </summary>
    [BsonCollection("logs")]
    public class LogDocument : BaseDocument
    {
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [BsonElement("level")]
        public string Level { get; set; } = string.Empty;
        
        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;
        
        [BsonElement("logger")]
        public string? Logger { get; set; }
        
        [BsonElement("exception")]
        public string? Exception { get; set; }
        
        [BsonElement("correlationId")]
        public string? CorrelationId { get; set; }
        
        [BsonElement("userId")]
        public long? UserId { get; set; }
        
        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }
        
        [BsonElement("userAgent")]
        public string? UserAgent { get; set; }
        
        [BsonElement("requestPath")]
        public string? RequestPath { get; set; }
        
        [BsonElement("requestMethod")]
        public string? RequestMethod { get; set; }
        
        [BsonElement("responseStatusCode")]
        public int? ResponseStatusCode { get; set; }
        
        [BsonElement("duration")]
        public long? Duration { get; set; }
        
        [BsonElement("properties")]
        public Dictionary<string, object>? Properties { get; set; }
        
        [BsonElement("tags")]
        public List<string>? Tags { get; set; }
    }
}
#endif