using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    public class Marshut
    {
        [Key]
        public long RouteId { get; set; }
        [Required]
        public string StartPoint { get; set; } // Non-nullable
        [Required]
        public string EndPoint { get; set; } // Non-nullable
        [ForeignKey("Employee")]
        public long DriverId { get; set; }
        public Employee Employee { get; set; } // Non-nullable navigation
        [ForeignKey("Avtobus")]
        public long BusId { get; set; }
        public Avtobus Avtobus { get; set; } // Non-nullable navigation
        public string TravelTime { get; set; } // Non-nullable
        public List<Bilet> Tickets { get; set; }

        // Add constructor for initialization
        public Marshut()
        {
            Tickets = new List<Bilet>();
        }
    }
} 