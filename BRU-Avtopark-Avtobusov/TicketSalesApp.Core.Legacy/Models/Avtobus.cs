using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    public class Avtobus
    {
        [Key]
        public long BusId { get; set; }
        [Required]
        public string Model { get; set; } // Non-nullable
        public List<Marshut> Routes { get; set; }
        public List<Obsluzhivanie> Obsluzhivanies { get; set; }

        public Avtobus()
        {
            Routes = new List<Marshut>();
            Obsluzhivanies = new List<Obsluzhivanie>();
        }
    }
} 