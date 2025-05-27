using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
#if MODERN
using System.Text.Json.Serialization;
#elif WINDOWSXP
using Newtonsoft.Json;
#endif

namespace TicketSalesApp.Core.Models
{
    public class Avtobus
    {
        [Key]
        public long BusId { get; set; }
        [Required]
        public string Model { get; set; }
        public List<Marshut> Routes { get; set; } = new List<Marshut>();
        public List<Obsluzhivanie> Obsluzhivanies { get; set; } = new List<Obsluzhivanie>();
    }
}