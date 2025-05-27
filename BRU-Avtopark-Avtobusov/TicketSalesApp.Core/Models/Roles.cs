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
{   // hopefully nothing blows up in code when i add usage of this to users???

    /// <summary>
    /// Represents a role in the system with associated permissions and metadata.
    /// </summary>
    public class Roles{
        [Key]
        public Guid RoleId { get; set; } // role id that ideally should be auto generated in both modes of api server providers - sql server and sqlite on development
        // is guid even a thing in sqlite? nope not officially supported, but it's still a thing if i make this type a string 
        // uhh maybe it could somehow be converted in code from guid type to string type?
        [Required]
        public int LegacyRoleId { get; set; } // legacy role id 0 - user, 1 - admin , maybe can be expanded to permit for more roles by hacking on top more numbers after 1
        [Required]
        public string Name { get; set; } // role name
        [Required]
        public string Description { get; set; } // role description
        [Required]
        public bool IsActive { get; set; } // role is active
        [Required]
        public DateTime CreatedAt { get; set; } // role created at
        [Required]
        public DateTime UpdatedAt { get; set; } // role updated at

        // New properties
#if MODERN
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        [StringLength(100)]
        public string? NormalizedName { get; set; }
#else
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        [StringLength(100)]
        public string NormalizedName { get; set; }
#endif

        public bool IsSystem { get; set; } = false;

        public int Priority { get; set; } = 0;

        // Navigation properties
#if MODERN
        public virtual ICollection<RolePermission>? RolePermissions { get; set; }
        public virtual ICollection<UserRole>? UserRoles { get; set; }
#else
        public virtual ICollection<RolePermission> RolePermissions { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; }
#endif

        // Computed property for role display
        [NotMapped]
        public string DisplayName => $"{Name} ({LegacyRoleId})";

        


    }

    

    /// <summary>
    /// Represents the many-to-many relationship between users and roles.
    /// </summary>
    public class UserRole
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }  // This will now map to User.GuidId

        [Required]
        public Guid RoleId { get; set; }

        [Required]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
#if MODERN
        public string? AssignedBy { get; set; }
#else
        public string AssignedBy { get; set; }
#endif

        // Navigation properties
#if MODERN
        public virtual User? User { get; set; }
        public virtual Roles? Role { get; set; }
#else
        public virtual User User { get; set; }
        public virtual Roles Role { get; set; }
#endif
    }
}