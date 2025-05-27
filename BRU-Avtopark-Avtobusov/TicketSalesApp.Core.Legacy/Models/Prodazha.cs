using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    public class Prodazha
    {
        [Key]
        public long SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        [ForeignKey("Bilet")]
        public long TicketId { get; set; }
        public Bilet Bilet { get; set; } // Non-nullable navigation
        
        public string TicketSoldToUser { get; set; }
        public string TicketSoldToUserPhone { get; set; }

        public Prodazha()
        {
            TicketSoldToUser = "ФИЗ.ПРОДАЖА";
            TicketSoldToUserPhone = "+375333000000";
        }
    }
} 