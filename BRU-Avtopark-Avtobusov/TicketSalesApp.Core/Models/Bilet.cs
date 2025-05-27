// Core/Models/Bilet.cs
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
    public class Bilet
    {
        [Key]
        public long TicketId { get; set; }
        [ForeignKey("Marshut")]
        public long RouteId { get; set; }
        public Marshut Marshut { get; set; }
        [Required]
        public decimal TicketPrice { get; set; }
        public List<Prodazha> Sales { get; set; } = new List<Prodazha>();
    }
}