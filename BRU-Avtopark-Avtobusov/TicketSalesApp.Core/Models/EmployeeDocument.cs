// Core/Models/EmployeeDocument.cs
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
    /// Represents employee documents such as passports, licenses, certificates, etc.
    /// Crucial for HR department tracking and compliance
    /// </summary>
    public class EmployeeDocument
    {
        [Key]
        public long DocumentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } // Passport, DriverLicense, MedicalCertificate, etc.

        [Required]
        [MaxLength(100)]
        public string DocumentNumber { get; set; }

        [Required]
        public DateTime IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(500)]
        public string IssuedBy { get; set; } // Organization that issued the document

        [MaxLength(1000)]
        public string FilePath { get; set; } // Path to scanned document file

        [MaxLength(2000)]
        public string Notes { get; set; }

        // Foreign key to Employee
        [Required]
        [ForeignKey("Employee")]
        public long EmployeeId { get; set; }

#if MODERN
        [JsonIgnore]
#endif
        public Employee Employee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
