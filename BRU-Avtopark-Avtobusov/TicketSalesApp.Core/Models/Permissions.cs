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
    /// Represents a permission in the system.
    /// </summary>
   public class Permission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid PermissionId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property - only the collection, no back reference
#if MODERN
        public virtual ICollection<RolePermission>? RolePermissions { get; set; }
#else
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
#endif
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
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
#if MODERN
        public string? GrantedBy { get; set; }
#else
        public string GrantedBy { get; set; }
#endif

        // Navigation properties without back references
#if MODERN
        public virtual Roles? Role { get; set; }
        public virtual Permission? Permission { get; set; }
#else
        public virtual Roles Role { get; set; }
        public virtual Permission Permission { get; set; }
#endif
    }
}