using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#if MODERN
using System.Text.Json.Serialization;
#elif WINDOWSXP
using Newtonsoft.Json;
#endif

namespace TicketSalesApp.Core.Models
{
    public class Obsluzhivanie
    {
        [Key]
        public long MaintenanceId { get; set; }
        [ForeignKey("Avtobus")]
        public long BusId { get; set; }
        public Avtobus Avtobus { get; set; }
        public DateTime LastServiceDate { get; set; }
        public string MileageThreshold { get; set; }
        public string MaintenanceType { get; set; }
        public string ServiceEngineer { get; set; }
        public string FoundIssues { get; set; }
        public DateTime NextServiceDate { get; set; }
        public string Roadworthiness { get; set; }
        

       
    }
}