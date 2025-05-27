using System;
using System.ComponentModel.DataAnnotations;

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    public class AdminActionLog
    {
        [Key]
        public long LogId { get; set; }
        [Required]
        public string UserId { get; set; } // Non-nullable
        [Required]
        public string Action { get; set; } // Non-nullable
        public string Details { get; set; } // Non-nullable
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } // Non-nullable
        public string UserAgent { get; set; } // Non-nullable
    }
} 