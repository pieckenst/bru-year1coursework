using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#if MODERN
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
#elif WINDOWSXP
// using System.Data.Entity; // Might be needed depending on DbContext implementation
using Newtonsoft.Json;
#endif

namespace TicketSalesApp.Core.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserId { get; set; }

        [Required]
        public Guid GuidId { get; set; } = Guid.NewGuid();

        [Required]
        public string Login { get; set; } // this is literally username field - with email being additional info only

#if MODERN
        public string? PhoneNumber { get; set; } = "+375333000000";

        public string? Email { get; set; } = "placeholderemail@mogilev.by";
#else
        public string PhoneNumber { get; set; } = "+375333000000";

        public string Email { get; set; } = "placeholderemail@mogilev.by";
#endif

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Obsolete("Use UserRoles collection instead. This property is kept for backward compatibility.")]
        [Required]
        public int Role { get; set; } // 0 - User, 1 - Admin

        // New properties
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

#if MODERN
        public DateTime? LastLoginAt { get; set; }
#else
        public DateTime? LastLoginAt { get; set; } // Nullable value types are okay
#endif

        public bool IsActive { get; set; } = true;

        // Navigation property for roles
#if MODERN
        public virtual ICollection<UserRole>? UserRoles { get; set; }
#else
        public virtual ICollection<UserRole> UserRoles { get; set; }
#endif

        public string? WindowsIdentity { get; set; }  // Store Windows SID or username
        public bool IsWindowsAuth { get; set; }      // Flag for Windows auth users
    }
}