using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Models // Adjusted namespace
{
    public class Obsluzhivanie
    {
        [Key]
        public long MaintenanceId { get; set; }
        [ForeignKey("Avtobus")]
        public long BusId { get; set; }
        public Avtobus Avtobus { get; set; } // Non-nullable navigation
        public DateTime LastServiceDate { get; set; }
        public string MileageThreshold { get; set; } // Non-nullable
        public string MaintenanceType { get; set; } // Non-nullable
        public string ServiceEngineer { get; set; } // Non-nullable
        public string FoundIssues { get; set; } // Non-nullable
        public DateTime NextServiceDate { get; set; }
        public string Roadworthiness { get; set; } // Non-nullable
    }
} 