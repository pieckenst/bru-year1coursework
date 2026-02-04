#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document representation of Marshut (Route) entity
    /// </summary>
    [BsonCollection("routes")]
    public class RouteDocument : BaseDocument
    {
        [BsonElement("routeId")]
        public long RouteId { get; set; }
        
        [BsonElement("routeName")]
        public string RouteName { get; set; } = string.Empty;
        
        [BsonElement("startPoint")]
        public string StartPoint { get; set; } = string.Empty;
        
        [BsonElement("endPoint")]
        public string EndPoint { get; set; } = string.Empty;
        
        [BsonElement("distance")]
        public double Distance { get; set; }
        
        [BsonElement("estimatedDuration")]
        public TimeSpan EstimatedDuration { get; set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
        
        [BsonElement("busId")]
        public long? BusId { get; set; }
        
        [BsonElement("driverId")]
        public long? DriverId { get; set; }
        
        [BsonElement("busInfo")]
        public BusReference? BusInfo { get; set; }
        
        [BsonElement("driverInfo")]
        public DriverReference? DriverInfo { get; set; }
        
        [BsonElement("stops")]
        public List<RouteStop>? Stops { get; set; }
        
        [BsonElement("schedules")]
        public List<RouteScheduleDocument>? Schedules { get; set; }
        
        [BsonElement("tickets")]
        public List<TicketReference>? Tickets { get; set; }
    }
    
    public class BusReference
    {
        [BsonElement("busId")]
        public long BusId { get; set; }
        
        [BsonElement("busNumber")]
        public string BusNumber { get; set; } = string.Empty;
        
        [BsonElement("model")]
        public string Model { get; set; } = string.Empty;
        
        [BsonElement("capacity")]
        public int Capacity { get; set; }
    }
    
    public class DriverReference
    {
        [BsonElement("employeeId")]
        public long EmployeeId { get; set; }
        
        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;
        
        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;
        
        [BsonElement("licenseNumber")]
        public string? LicenseNumber { get; set; }
    }
    
    public class RouteStop
    {
        [BsonElement("stopId")]
        public int StopId { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("latitude")]
        public double? Latitude { get; set; }
        
        [BsonElement("longitude")]
        public double? Longitude { get; set; }
        
        [BsonElement("estimatedArrivalTime")]
        public TimeSpan? EstimatedArrivalTime { get; set; }
        
        [BsonElement("distanceFromStart")]
        public double? DistanceFromStart { get; set; }
    }
    
    public class RouteScheduleDocument
    {
        [BsonElement("scheduleId")]
        public long ScheduleId { get; set; }
        
        [BsonElement("departureTime")]
        public TimeSpan DepartureTime { get; set; }
        
        [BsonElement("arrivalTime")]
        public TimeSpan ArrivalTime { get; set; }
        
        [BsonElement("daysOfWeek")]
        public List<string> DaysOfWeek { get; set; } = new();
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
        
        [BsonElement("frequency")]
        public int? Frequency { get; set; }
        
        [BsonElement("validFrom")]
        public DateTime? ValidFrom { get; set; }
        
        [BsonElement("validTo")]
        public DateTime? ValidTo { get; set; }
    }
    
    public class TicketReference
    {
        [BsonElement("ticketId")]
        public long TicketId { get; set; }
        
        [BsonElement("ticketPrice")]
        public decimal TicketPrice { get; set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
#endif