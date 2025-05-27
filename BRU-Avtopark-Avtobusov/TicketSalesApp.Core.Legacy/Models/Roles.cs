using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    /// <summary>
    /// Represents a role in the system with associated permissions and metadata.
    /// </summary>
    public class Roles{
        [Key]
        public Guid RoleId { get; set; }
        [Required]
        public int LegacyRoleId { get; set; }
        [Required]
        public string Name { get; set; } // Non-nullable
        [Required]
        public string Description { get; set; } // Non-nullable
        [Required]
        public bool IsActive { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        [Required]
        public DateTime UpdatedAt { get; set; }

        // New properties (made non-nullable for net40)
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        [StringLength(100)]
        public string NormalizedName { get; set; }

        public bool IsSystem { get; set; }

        public int Priority { get; set; }

        // Navigation properties (made non-nullable for net40)
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; }

        // Computed property for role display
        [NotMapped]
        public string DisplayName { get { return string.Format("{0} ({1})", Name, LegacyRoleId); } }

        // Add constructor for initialization
        public Roles()
        {
            IsSystem = false;
            Priority = 0;
            // Initialize collections
            RolePermissions = new List<RolePermission>();
            UserRoles = new List<UserRole>();
        }
    }

    /// <summary>
    /// Represents the many-to-many relationship between users and roles.
    /// </summary>
    public class UserRole
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }  // This will now map to User.GuidId

        [Required]
        public Guid RoleId { get; set; }

        [Required]
        public DateTime AssignedAt { get; set; }
        public string AssignedBy { get; set; } // Non-nullable

        // Navigation properties (made non-nullable for net40)
        public virtual User User { get; set; }
        public virtual Roles Role { get; set; }

        // Add constructor for initialization
        public UserRole()
        {
            Id = Guid.NewGuid();
            AssignedAt = DateTime.UtcNow;
        }
    }
} 