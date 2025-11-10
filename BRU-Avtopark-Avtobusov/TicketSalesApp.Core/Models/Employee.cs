// Core/Models/Employee.cs
using System;
using System.Collections.Generic;
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
    /// Enhanced Employee model with comprehensive HR features for transportation industry
    /// </summary>
    public class Employee
    {
        [Key]
        public long EmpId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Surname { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(100)]
        public string Patronym { get; set; }
        
        [Required]
        public DateTime EmployedSince { get; set; }
        
        [ForeignKey("Job")]
        public long JobId { get; set; }
        
        public Job Job { get; set; }

        // === HR Department Information ===
        
        // Personal Information
        [MaxLength(20)]
        public string PassportSeries { get; set; }
        
        [MaxLength(20)]
        public string PassportNumber { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [MaxLength(500)]
        public string Address { get; set; }
        
        [MaxLength(20)]
        public string PersonalPhone { get; set; }
        
        [MaxLength(20)]
        public string WorkPhone { get; set; }
        
        [MaxLength(200)]
        public string Email { get; set; }
        
        // Tax and Social Security
        [MaxLength(20)]
        public string INN { get; set; } // Tax identification number (ИНН)
        
        [MaxLength(20)]
        public string SNILS { get; set; } // Pension insurance number (СНИЛС)
        
        // === Transportation Industry Specific ===
        
        // Driver-specific information
        [MaxLength(20)]
        public string DriverLicenseNumber { get; set; }
        
        [MaxLength(50)]
        public string DriverLicenseCategory { get; set; } // B, C, D, etc.
        
        public DateTime? DriverLicenseIssueDate { get; set; }
        
        public DateTime? DriverLicenseExpiryDate { get; set; }
        
        // Medical certification (required for drivers)
        [MaxLength(50)]
        public string MedicalCertificateNumber { get; set; }
        
        public DateTime? MedicalCertificateIssueDate { get; set; }
        
        public DateTime? MedicalCertificateExpiryDate { get; set; }
        
        public DateTime? LastMedicalCheckDate { get; set; }
        
        public DateTime? NextMedicalCheckDate { get; set; }
        
        // Special certifications
        public bool HasPassengerTransportCertification { get; set; }
        
        public bool HasDangerousGoodsCertification { get; set; }
        
        // === Organizational Structure ===
        
        [ForeignKey("Department")]
        public long? DepartmentId { get; set; }
        
#if MODERN
        public Department? Department { get; set; }
#else
        public Department Department { get; set; }
#endif
        
        // === Work Status ===
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? TerminationDate { get; set; }
        
        [MaxLength(1000)]
        public string TerminationReason { get; set; }
        
        // === Navigation Properties for Related Data ===
        
#if MODERN
        [JsonIgnore]
        public List<EmployeeDocument>? Documents { get; set; }
        
        [JsonIgnore]
        public List<EmployeeTraining>? Trainings { get; set; }
        
        [JsonIgnore]
        public List<EmergencyContact>? EmergencyContacts { get; set; }
        
        [JsonIgnore]
        public List<VacationRequest>? VacationRequests { get; set; }
#else
        [JsonIgnore]
        public List<EmployeeDocument> Documents { get; set; }
        
        [JsonIgnore]
        public List<EmployeeTraining> Trainings { get; set; }
        
        [JsonIgnore]
        public List<EmergencyContact> EmergencyContacts { get; set; }
        
        [JsonIgnore]
        public List<VacationRequest> VacationRequests { get; set; }
#endif
        
        // === Audit Fields ===
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
}