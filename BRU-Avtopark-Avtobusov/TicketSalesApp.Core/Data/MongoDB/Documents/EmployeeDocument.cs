#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document representation of Employee entity
    /// </summary>
    [BsonCollection("employees")]
    public class EmployeeDocument : BaseDocument
    {
        [BsonElement("employeeId")]
        public long EmployeeId { get; set; }
        
        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;
        
        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;
        
        [BsonElement("middleName")]
        public string? MiddleName { get; set; }
        
        [BsonElement("dateOfBirth")]
        public DateTime? DateOfBirth { get; set; }
        
        [BsonElement("phoneNumber")]
        public string? PhoneNumber { get; set; }
        
        [BsonElement("email")]
        public string? Email { get; set; }
        
        [BsonElement("address")]
        public string? Address { get; set; }
        
        [BsonElement("hireDate")]
        public DateTime HireDate { get; set; }
        
        [BsonElement("terminationDate")]
        public DateTime? TerminationDate { get; set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
        
        [BsonElement("jobId")]
        public long JobId { get; set; }
        
        [BsonElement("departmentId")]
        public long? DepartmentId { get; set; }
        
        [BsonElement("salary")]
        public decimal? Salary { get; set; }
        
        [BsonElement("licenseNumber")]
        public string? LicenseNumber { get; set; }
        
        [BsonElement("licenseExpiryDate")]
        public DateTime? LicenseExpiryDate { get; set; }
        
        [BsonElement("jobInfo")]
        public JobReference? JobInfo { get; set; }
        
        [BsonElement("departmentInfo")]
        public DepartmentReference? DepartmentInfo { get; set; }
        
        [BsonElement("documents")]
        public List<EmployeeDocumentReference>? Documents { get; set; }
        
        [BsonElement("trainings")]
        public List<EmployeeTrainingReference>? Trainings { get; set; }
        
        [BsonElement("emergencyContacts")]
        public List<EmergencyContactReference>? EmergencyContacts { get; set; }
        
        [BsonElement("vacationRequests")]
        public List<VacationRequestReference>? VacationRequests { get; set; }
    }
    
    public class JobReference
    {
        [BsonElement("jobId")]
        public long JobId { get; set; }
        
        [BsonElement("jobTitle")]
        public string JobTitle { get; set; } = string.Empty;
        
        [BsonElement("description")]
        public string? Description { get; set; }
        
        [BsonElement("baseSalary")]
        public decimal? BaseSalary { get; set; }
    }
    
    public class DepartmentReference
    {
        [BsonElement("departmentId")]
        public long DepartmentId { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("description")]
        public string? Description { get; set; }
        
        [BsonElement("managerId")]
        public long? ManagerId { get; set; }
    }
    
    public class EmployeeDocumentReference
    {
        [BsonElement("documentId")]
        public long DocumentId { get; set; }
        
        [BsonElement("documentType")]
        public string DocumentType { get; set; } = string.Empty;
        
        [BsonElement("documentName")]
        public string DocumentName { get; set; } = string.Empty;
        
        [BsonElement("filePath")]
        public string? FilePath { get; set; }
        
        [BsonElement("uploadDate")]
        public DateTime UploadDate { get; set; }
        
        [BsonElement("expiryDate")]
        public DateTime? ExpiryDate { get; set; }
    }
    
    public class EmployeeTrainingReference
    {
        [BsonElement("trainingId")]
        public long TrainingId { get; set; }
        
        [BsonElement("trainingName")]
        public string TrainingName { get; set; } = string.Empty;
        
        [BsonElement("provider")]
        public string? Provider { get; set; }
        
        [BsonElement("completionDate")]
        public DateTime CompletionDate { get; set; }
        
        [BsonElement("expiryDate")]
        public DateTime? ExpiryDate { get; set; }
        
        [BsonElement("certificateNumber")]
        public string? CertificateNumber { get; set; }
    }
    
    public class EmergencyContactReference
    {
        [BsonElement("contactId")]
        public long ContactId { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("relationship")]
        public string Relationship { get; set; } = string.Empty;
        
        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [BsonElement("email")]
        public string? Email { get; set; }
        
        [BsonElement("address")]
        public string? Address { get; set; }
    }
    
    public class VacationRequestReference
    {
        [BsonElement("requestId")]
        public long RequestId { get; set; }
        
        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }
        
        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }
        
        [BsonElement("requestDate")]
        public DateTime RequestDate { get; set; }
        
        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;
        
        [BsonElement("reason")]
        public string? Reason { get; set; }
        
        [BsonElement("approvedBy")]
        public long? ApprovedBy { get; set; }
        
        [BsonElement("approvalDate")]
        public DateTime? ApprovalDate { get; set; }
    }
}
#endif