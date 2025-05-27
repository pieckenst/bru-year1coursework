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
    public class Prodazha
    {
        [Key]
        public long SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        [ForeignKey("Bilet")]
        public long TicketId { get; set; }
        public Bilet Bilet { get; set; }
        
        public string TicketSoldToUser { get; set; } = "ФИЗ.ПРОДАЖА";
        public string TicketSoldToUserPhone { get; set; } = "+375333000000";  // USUALLY THIS IS THE PHONE NUMBER OF THE USER WHO  
                                                                              // THE TICKET WAS SOLD TO SO THE TICKET BUYER- 
                                                                              // THATS HOW IN BELARUS THE TICKET RESERVATIONS ARE DONE
    }
}