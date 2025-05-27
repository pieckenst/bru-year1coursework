using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
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
        public List<Prodazha> Sales { get; set; }

        public Bilet()
        {
            Sales = new List<Prodazha>();
        }
    }
}