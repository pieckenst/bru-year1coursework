using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#if MODERN
using System.Text.Json.Serialization;
#elif WINDOWSXP
using Newtonsoft.Json;
#endif

namespace TicketSalesApp.Core.Models
{ 
    public class RouteSchedules {
        [Key]
        public long RouteScheduleId { get; set; }  

#if MODERN
        [Required]
        public string? StartPoint { get; set; }  
        [Required]
        public string[]? RouteStops { get; set; }  

        [Required]
        public string? EndPoint { get; set; }  
#else
        [Required]
        public string StartPoint { get; set; }
        [Required]
        public string[] RouteStops { get; set; }

        [Required]
        public string EndPoint { get; set; }
#endif
        [Required]
        public DateTime DepartureTime { get; set; }  

        [Required]
        public DateTime ArrivalTime { get; set; }  

        [Required]
        public double? Price { get; set; }  

        [Required]
        public int? AvailableSeats { get; set; }  

#if MODERN
        [Required]
        public string[]? DaysOfWeek { get; set; }  

        [Required]
        public string[]? BusTypes { get; set; }  
#else
        [Required]
        public string[] DaysOfWeek { get; set; }

        [Required]
        public string[] BusTypes { get; set; }
#endif

        // Link to Marshut
        [ForeignKey("Marshut")]
        public long? RouteId { get; set; }
#if MODERN
        public Marshut? Marshut { get; set; }
#else
        public Marshut Marshut { get; set; }
#endif

        // Additional useful fields
        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime ValidFrom { get; set; } = DateTime.Now;

        public DateTime? ValidUntil { get; set; }

        [Required]
        public int? StopDurationMinutes { get; set; } = 5;

        [Required]
        public bool IsRecurring { get; set; } = true;

        // Calculated properties
        [NotMapped]
        public TimeSpan TotalTravelTime => ArrivalTime - DepartureTime;

        [NotMapped]
        public int TotalStops => RouteStops?.Length ?? 0;

        [NotMapped]
        public bool IsCurrentlyValid => 
            IsActive && 
            ValidFrom <= DateTime.Now && 
            (!ValidUntil.HasValue || ValidUntil.Value >= DateTime.Now);

        // For storing estimated arrival times at each stop
#if MODERN
        public string[]? EstimatedStopTimes { get; set; }
#else
        public string[] EstimatedStopTimes { get; set; }
#endif

        // For storing distances between stops in kilometers
#if MODERN
        public double[]? StopDistances { get; set; }
#else
        public double[] StopDistances { get; set; }
#endif

        // For storing any special notes about the schedule
#if MODERN
        public string? Notes { get; set; }
#else
        public string Notes { get; set; }
#endif

        // For tracking schedule changes
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
#if MODERN
        public string? UpdatedBy { get; set; }
#else
        public string UpdatedBy { get; set; }
#endif
    }
}