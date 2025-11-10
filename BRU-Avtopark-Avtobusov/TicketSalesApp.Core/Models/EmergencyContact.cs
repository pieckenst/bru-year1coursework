// Core/Models/EmergencyContact.cs
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
    /// Emergency contact information for employees
    /// Critical for driver and field staff safety in transportation industry
    /// </summary>
    public class EmergencyContact
    {
        [Key]
        public long ContactId { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public long EmployeeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContactName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Relationship { get; set; } // Spouse, Parent, Sibling, etc.

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        [MaxLength(20)]
        public string AlternatePhoneNumber { get; set; }

        [MaxLength(500)]
        public string Address { get; set; }

        public bool IsPrimary { get; set; } // Primary emergency contact

#if MODERN
        [JsonIgnore]
        public Employee? Employee { get; set; }
#else
        [JsonIgnore]
        public Employee Employee { get; set; }
#endif
    }
}
