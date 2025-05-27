// Core/Models/Marshut.cs
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
    public class Marshut
    {
        [Key]
        public long RouteId { get; set; }
        [Required]
        public string StartPoint { get; set; }
        [Required]
        public string EndPoint { get; set; }
        [ForeignKey("Employee")]
        public long DriverId { get; set; }
        public Employee Employee { get; set; }
        [ForeignKey("Avtobus")]
        public long BusId { get; set; }
        public Avtobus Avtobus { get; set; }
        public string TravelTime { get; set; }
        public List<Bilet> Tickets { get; set; } = new List<Bilet>();
    }
}