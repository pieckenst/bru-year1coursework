// Core/Models/Department.cs
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
    /// Represents organizational departments within the transportation company
    /// Supports hierarchical structure for proper HR management
    /// </summary>
    public class Department
    {
        [Key]
        public long DepartmentId { get; set; }

        [Required]
        [MaxLength(200)]
        public string DepartmentName { get; set; }

        [MaxLength(50)]
        public string DepartmentCode { get; set; } // Short code for department

        [MaxLength(1000)]
        public string Description { get; set; }

        // Self-referencing foreign key for hierarchical structure
        [ForeignKey("ParentDepartment")]
        public long? ParentDepartmentId { get; set; }

#if MODERN
        [JsonIgnore]
        public Department? ParentDepartment { get; set; }

        public List<Department>? ChildDepartments { get; set; }

        public List<Employee>? Employees { get; set; }
#else
        [JsonIgnore]
        public Department ParentDepartment { get; set; }

        public List<Department> ChildDepartments { get; set; }

        public List<Employee> Employees { get; set; }
#endif

        public bool IsActive { get; set; } = true;
    }
}
