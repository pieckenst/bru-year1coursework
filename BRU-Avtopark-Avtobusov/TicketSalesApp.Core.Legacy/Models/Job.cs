using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    public class Job
    {
        [Key]
        public long JobId { get; set; }
        [Required]
        public string JobTitle { get; set; } // Non-nullable
        public string Internship { get; set; } // Non-nullable
        
        public List<Employee> Employees { get; set; }

        // Add constructor for initialization
        public Job()
        {
            Employees = new List<Employee>();
        }
    }
} 