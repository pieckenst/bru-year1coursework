// Core/Models/VacationRequest.cs
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
    /// <summary>
    /// Represents employee vacation and leave requests
    /// Essential for workforce planning in transportation industry
    /// </summary>
    public class VacationRequest
    {
        [Key]
        public long RequestId { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public long EmployeeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string VacationType { get; set; } // Annual, Sick, Unpaid, etc.

        [MaxLength(2000)]
        public string Reason { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } // Pending, Approved, Rejected, Cancelled

        [ForeignKey("ApprovedBy")]
        public long? ApprovedByUserId { get; set; }

        public DateTime? ApprovalDate { get; set; }

        [MaxLength(2000)]
        public string ApprovalNotes { get; set; }

        public int DaysRequested { get; set; }

#if MODERN
        [JsonIgnore]
        public Employee? Employee { get; set; }

        [JsonIgnore]
        public User? ApprovedBy { get; set; }
#else
        [JsonIgnore]
        public Employee Employee { get; set; }

        [JsonIgnore]
        public User ApprovedBy { get; set; }
#endif

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
