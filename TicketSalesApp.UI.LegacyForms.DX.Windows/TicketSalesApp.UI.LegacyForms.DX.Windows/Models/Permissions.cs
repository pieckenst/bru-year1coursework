using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Models // Adjusted namespace
{
    /// <summary>
    /// Represents a permission in the system.
    /// </summary>
    public class Permission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid PermissionId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } 

        [Required]
        [StringLength(200)]
        public string Description { get; set; } 

        [Required]
        public string Category { get; set; }

        public bool IsActive { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation property - only the collection, no back reference
        public virtual ICollection<RolePermission> RolePermissions { get; set; }

        // Add constructor for initialization
        public Permission()
        {
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            // Initialize collection properties if they exist (though not explicitly initialized before)
            RolePermissions = new List<RolePermission>();
        }
    }

    /// <summary>
    /// Represents the many-to-many relationship between roles and permissions.
    /// </summary>
    public class RolePermission
    {
        [Key]
        [Column(Order = 1)]
        public Guid RoleId { get; set; }

        [Key]
        [Column(Order = 2)]
        public Guid PermissionId { get; set; }

        [Required]
        public DateTime GrantedAt { get; set; }
        public string GrantedBy { get; set; } // Non-nullable

        // Navigation properties without back references
        public virtual Roles Role { get; set; } // Non-nullable
        public virtual Permission Permission { get; set; } // Non-nullable

        // Add constructor for initialization
        public RolePermission()
        {
            GrantedAt = DateTime.UtcNow;
        }
    }
} 