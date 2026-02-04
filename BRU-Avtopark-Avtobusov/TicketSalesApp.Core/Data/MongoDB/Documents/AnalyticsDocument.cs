#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document for analytics data
    /// </summary>
    [BsonCollection("analytics")]
    public class AnalyticsDocument : BaseDocument
    {
        [BsonElement("eventType")]
        public string EventType { get; set; } = string.Empty;
        
        [BsonElement("eventName")]
        public string EventName { get; set; } = string.Empty;
        
        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [BsonElement("userId")]
        public long? UserId { get; set; }
        
        [BsonElement("sessionId")]
        public string? SessionId { get; set; }
        
        [BsonElement("ipAddress")]
        public string? IpAddress { get; set; }
        
        [BsonElement("userAgent")]
        public string? UserAgent { get; set; }
        
        [BsonElement("referrer")]
        public string? Referrer { get; set; }
        
        [BsonElement("entityType")]
        public string? EntityType { get; set; }
        
        [BsonElement("entityId")]
        public long? EntityId { get; set; }
        
        [BsonElement("action")]
        public string? Action { get; set; }
        
        [BsonElement("properties")]
        public Dictionary<string, object>? Properties { get; set; }
        
        [BsonElement("metrics")]
        public Dictionary<string, double>? Metrics { get; set; }
        
        [BsonElement("tags")]
        public List<string>? Tags { get; set; }
        
        [BsonElement("geolocation")]
        public GeolocationData? Geolocation { get; set; }
        
        [BsonElement("deviceInfo")]
        public DeviceInfo? DeviceInfo { get; set; }
    }
    
    public class GeolocationData
    {
        [BsonElement("country")]
        public string? Country { get; set; }
        
        [BsonElement("region")]
        public string? Region { get; set; }
        
        [BsonElement("city")]
        public string? City { get; set; }
        
        [BsonElement("latitude")]
        public double? Latitude { get; set; }
        
        [BsonElement("longitude")]
        public double? Longitude { get; set; }
    }
    
    public class DeviceInfo
    {
        [BsonElement("deviceType")]
        public string? DeviceType { get; set; }
        
        [BsonElement("operatingSystem")]
        public string? OperatingSystem { get; set; }
        
        [BsonElement("browser")]
        public string? Browser { get; set; }
        
        [BsonElement("screenResolution")]
        public string? ScreenResolution { get; set; }
        
        [BsonElement("isMobile")]
        public bool? IsMobile { get; set; }
    }
}
#endif