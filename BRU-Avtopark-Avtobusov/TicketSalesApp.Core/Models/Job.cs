// Core/Models/Job.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
#if MODERN
using System.Text.Json.Serialization;
#elif WINDOWSXP
using Newtonsoft.Json;
#endif

namespace TicketSalesApp.Core.Models
{
    public class Job
    {
        [Key]
        public long JobId { get; set; }
        [Required]
        public string JobTitle { get; set; }
        public string Internship { get; set; }
        
        public List<Employee> Employees { get; set; } = new List<Employee>();
    }
}