// Core/Models/Employee.cs
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
    public class Employee
    {
        [Key]
        public long EmpId { get; set; }
        [Required]
        public string Surname { get; set; }
        [Required]
        public string Name { get; set; }
        public string Patronym { get; set; }
        public DateTime EmployedSince { get; set; }
        [ForeignKey("Job")]
        public long JobId { get; set; }
       
        public Job Job { get; set; }
    }
}