// Core/Models/AdminActionLog.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace TicketSalesApp.Core.Models
{
    public class AdminActionLog
    {
        [Key]
        public long LogId { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}