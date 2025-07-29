using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSalesApp.Core.Models
{
    public class FormDefinition
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        
        [Required]
        public string JsonSchema { get; set; }
        
        public string Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        [Required]
        public string CreatedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
