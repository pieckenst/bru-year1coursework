// Core/Models/EmployeeTraining.cs
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
    /// Represents employee training and certifications
    /// Critical for transportation industry compliance and safety
    /// </summary>
    public class EmployeeTraining
    {
        [Key]
        public long TrainingId { get; set; }

        [Required]
        [MaxLength(200)]
        public string TrainingName { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        public DateTime CompletionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(100)]
        public string CertificateNumber { get; set; }

        [MaxLength(500)]
        public string IssuingOrganization { get; set; }

        public bool IsMandatory { get; set; } // Required by law or company policy

        [MaxLength(1000)]
        public string FilePath { get; set; } // Path to certificate file

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
