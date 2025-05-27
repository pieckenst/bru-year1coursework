using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using System.Data.Entity; // Not needed in model file itself
// using Newtonsoft.Json; // Not needed unless attributes are used

namespace TicketSalesApp.Core.Legacy.Models // Adjusted namespace
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserId { get; set; }

        [Required]
        public Guid GuidId { get; set; } 

        [Required]
        public string Login { get; set; } // Non-nullable

        public string PhoneNumber { get; set; } 
      public string Email { get; set; } 

        [Required]
        public string PasswordHash { get; set; }

        [Obsolete("Use UserRoles collection instead. This property is kept for backward compatibility.")]
        [Required]
        public int Role { get; set; } // 0 - User, 1 - Admin

        // New properties
        [Required]
        public DateTime CreatedAt { get; set; } 

        public DateTime? LastLoginAt { get; set; } // Nullable value type is fine

        public bool IsActive { get; set; } 

        // Navigation property for roles (made non-nullable for net40)
        public virtual ICollection<UserRole> UserRoles { get; set; }

        // Add constructor for initialization
        public User()
        {
            GuidId = Guid.NewGuid();
            PhoneNumber = "+375333000000";
            Email = "placeholderemail@mogilev.by";
            PasswordHash = string.Empty;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            // Initialize collection
            UserRoles = new List<UserRole>();
        }
    }
} 