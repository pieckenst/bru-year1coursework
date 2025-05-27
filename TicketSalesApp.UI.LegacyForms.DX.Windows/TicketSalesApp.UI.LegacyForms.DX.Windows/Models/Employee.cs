using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Models // Adjusted namespace
{
    public class Employee
    {
        [Key]
        public long EmpId { get; set; }
        [Required]
        public string Surname { get; set; } // Non-nullable
        [Required]
        public string Name { get; set; } // Non-nullable
        public string Patronym { get; set; } // Non-nullable
        public DateTime EmployedSince { get; set; }
        [ForeignKey("Job")]
        public long JobId { get; set; }
       
        public Job Job { get; set; } // Non-nullable navigation
    }
} 