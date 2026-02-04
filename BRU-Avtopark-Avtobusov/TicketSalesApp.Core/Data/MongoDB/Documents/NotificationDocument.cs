#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document for notification tracking
    /// </summary>
    [BsonCollection("notifications")]
    public class NotificationDocument : BaseDocument
    {
        [BsonElement("notificationId")]
        public Guid NotificationId { get; set; }
        
        [BsonElement("userId")]
        public long? UserId { get; set; }
        
        [BsonElement("groupName")]
        public string? GroupName { get; set; }
        
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;
        
        [BsonElement("title")]
        public string? Title { get; set; }
        
        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;
        
        [BsonElement("data")]
        public Dictionary<string, object>? Data { get; set; }
        
        [BsonElement("priority")]
        public string Priority { get; set; } = "Normal";
        
        [BsonElement("status")]
        public string Status { get; set; } = "Pending";
        
        [BsonElement("deliveryMethod")]
        public List<string> DeliveryMethod { get; set; } = new();
        
        [BsonElement("scheduledAt")]
        public DateTime? ScheduledAt { get; set; }
        
        [BsonElement("sentAt")]
        public DateTime? SentAt { get; set; }
        
        [BsonElement("deliveredAt")]
        public DateTime? DeliveredAt { get; set; }
        
        [BsonElement("readAt")]
        public DateTime? ReadAt { get; set; }
        
        [BsonElement("expiresAt")]
        public DateTime? ExpiresAt { get; set; }
        
        [BsonElement("retryCount")]
        public int RetryCount { get; set; }
        
        [BsonElement("maxRetries")]
        public int MaxRetries { get; set; } = 3;
        
        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }
        
        [BsonElement("metadata")]
        public NotificationMetadata? Metadata { get; set; }
        
        [BsonElement("deliveryResults")]
        public List<DeliveryResult>? DeliveryResults { get; set; }
    }
    
    public class NotificationMetadata
    {
        [BsonElement("source")]
        public string? Source { get; set; }
        
        [BsonElement("category")]
        public string? Category { get; set; }
        
        [BsonElement("tags")]
        public List<string>? Tags { get; set; }
        
        [BsonElement("correlationId")]
        public string? CorrelationId { get; set; }
        
        [BsonElement("entityType")]
        public string? EntityType { get; set; }
        
        [BsonElement("entityId")]
        public long? EntityId { get; set; }
        
        [BsonElement("actionUrl")]
        public string? ActionUrl { get; set; }
        
        [BsonElement("iconUrl")]
        public string? IconUrl { get; set; }
    }
    
    public class DeliveryResult
    {
        [BsonElement("method")]
        public string Method { get; set; } = string.Empty;
        
        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;
        
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [BsonElement("errorMessage")]
        public string? ErrorMessage { get; set; }
        
        [BsonElement("deliveryId")]
        public string? DeliveryId { get; set; }
        
        [BsonElement("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
#endif