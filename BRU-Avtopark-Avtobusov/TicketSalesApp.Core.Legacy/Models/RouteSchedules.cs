using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes/specific serialization is used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    public class RouteSchedules
    {
        [Key]
        public long RouteScheduleId { get; set; }

        [Required]
        public string StartPoint { get; set; } // Non-nullable
        [Required]
        public string[] RouteStops { get; set; } // Note: EF6 might require custom handling/mapping for arrays

        [Required]
        public string EndPoint { get; set; } // Non-nullable
        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public DateTime ArrivalTime { get; set; }

        [Required]
        public double? Price { get; set; } // Nullable value type is fine

        [Required]
        public int? AvailableSeats { get; set; } // Nullable value type is fine

        [Required]
        public string[] DaysOfWeek { get; set; } // Note: EF6 might require custom handling/mapping for arrays

        [Required]
        public string[] BusTypes { get; set; } // Note: EF6 might require custom handling/mapping for arrays

        // Link to Marshut
        [ForeignKey("Marshut")]
        public long? RouteId { get; set; } // Nullable value type is fine
        public Marshut Marshut { get; set; } // Non-nullable navigation

        // Additional useful fields
        [Required]
        public bool IsActive { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        public DateTime? ValidUntil { get; set; } // Nullable value type is fine

        [Required]
        public int? StopDurationMinutes { get; set; } // Nullable value type is fine

        [Required]
        public bool IsRecurring { get; set; } // Nullable value type is fine

        // Calculated properties
        [NotMapped]
        public TimeSpan TotalTravelTime
        {
            get { return ArrivalTime - DepartureTime; }
        }

        [NotMapped]
        public int TotalStops
        {
            get { return RouteStops != null ? RouteStops.Length : 0; } // Basic null check for array
        }

        [NotMapped]
        public bool IsCurrentlyValid
        {
            get 
            {
                 return IsActive && 
                        ValidFrom <= DateTime.Now && 
                        (!ValidUntil.HasValue || ValidUntil.Value >= DateTime.Now);
            }
        }

        // For storing estimated arrival times at each stop
        public string[] EstimatedStopTimes { get; set; } // Note: EF6 might require custom handling/mapping for arrays

        // For storing distances between stops in kilometers
        public double[] StopDistances { get; set; } // Note: EF6 might require custom handling/mapping for arrays

        // For storing any special notes about the schedule
        public string Notes { get; set; } // Non-nullable

        // For tracking schedule changes
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; } // Nullable value type is fine
        public string UpdatedBy { get; set; } // Non-nullable

        // Add constructor for initialization
        public RouteSchedules()
        {
            IsActive = true;
            ValidFrom = DateTime.Now;
            StopDurationMinutes = 5;
            IsRecurring = true;
            CreatedAt = DateTime.Now;
            // Initialize arrays/collections if needed (though not explicitly initialized before)
            RouteStops = new string[0];
            DaysOfWeek = new string[0];
            BusTypes = new string[0];
            EstimatedStopTimes = new string[0];
            StopDistances = new double[0];
        }
    }
} 