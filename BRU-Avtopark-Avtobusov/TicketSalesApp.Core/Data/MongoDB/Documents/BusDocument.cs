#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document representation of Avtobus (Bus) entity
    /// </summary>
    [BsonCollection("buses")]
    public class BusDocument : BaseDocument
    {
        [BsonElement("busId")]
        public long BusId { get; set; }
        
        [BsonElement("busNumber")]
        public string BusNumber { get; set; } = string.Empty;
        
        [BsonElement("model")]
        public string Model { get; set; } = string.Empty;
        
        [BsonElement("capacity")]
        public int Capacity { get; set; }
        
        [BsonElement("yearManufactured")]
        public int YearManufactured { get; set; }
        
        [BsonElement("licensePlate")]
        public string? LicensePlate { get; set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
        
        [BsonElement("lastMaintenanceDate")]
        public DateTime? LastMaintenanceDate { get; set; }
        
        [BsonElement("nextMaintenanceDate")]
        public DateTime? NextMaintenanceDate { get; set; }
        
        [BsonElement("mileage")]
        public double? Mileage { get; set; }
        
        [BsonElement("fuelType")]
        public string? FuelType { get; set; }
        
        [BsonElement("routes")]
        public List<RouteReference>? Routes { get; set; }
        
        [BsonElement("maintenanceHistory")]
        public List<MaintenanceRecord>? MaintenanceHistory { get; set; }
    }
    
    public class RouteReference
    {
        [BsonElement("routeId")]
        public long RouteId { get; set; }
        
        [BsonElement("routeName")]
        public string RouteName { get; set; } = string.Empty;
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
    
    public class MaintenanceRecord
    {
        [BsonElement("maintenanceId")]
        public long MaintenanceId { get; set; }
        
        [BsonElement("date")]
        public DateTime Date { get; set; }
        
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;
        
        [BsonElement("description")]
        public string? Description { get; set; }
        
        [BsonElement("cost")]
        public decimal? Cost { get; set; }
        
        [BsonElement("performedBy")]
        public string? PerformedBy { get; set; }
    }
}
#endif