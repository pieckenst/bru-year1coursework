using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSalesApp.Core.Models
{
    /// <summary>
    /// Represents a Wii-style channel configuration for the customer UI
    /// </summary>
    [Table("WiiChannels")]
    public class WiiChannel
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique identifier for the channel (used in HTML/JS)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ChannelKey { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the channel
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description shown in channel details
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Position in the menu grid (1-12)
        /// </summary>
        [Range(1, 12)]
        public int Position { get; set; }

        /// <summary>
        /// Whether the channel is active/visible
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Channel icon image path/URL
        /// </summary>
        [StringLength(500)]
        public string? IconPath { get; set; }

        /// <summary>
        /// Sprite ID for channel icon (uses channel-{spriteId}.png from spritesheet)
        /// </summary>
        [StringLength(50)]
        public string? SpriteId { get; set; }

        /// <summary>
        /// Splash screen background image path/URL
        /// </summary>
        [StringLength(500)]
        public string? SplashImagePath { get; set; }

        /// <summary>
        /// Custom splash screen HTML content
        /// </summary>
        public string? SplashHtmlContent { get; set; }

        /// <summary>
        /// CSS classes to apply to the splash screen
        /// </summary>
        [StringLength(200)]
        public string? SplashCssClasses { get; set; }

        /// <summary>
        /// Action to perform when channel is activated (URL, view, or custom action)
        /// </summary>
        [StringLength(500)]
        public string? ActionUrl { get; set; }

        /// <summary>
        /// Action type: 'url', 'view', 'javascript', 'api'
        /// </summary>
        [StringLength(20)]
        public string ActionType { get; set; } = "url";

        /// <summary>
        /// Custom JavaScript to execute on channel activation
        /// </summary>
        public string? CustomJavaScript { get; set; }

        /// <summary>
        /// JSON configuration for advanced channel settings
        /// </summary>
        public string? ConfigurationJson { get; set; }

        /// <summary>
        /// Display order for sorting
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Channel category/group
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// Custom splash screen title
        /// </summary>
        [StringLength(100)]
        public string? SplashTitle { get; set; }

        /// <summary>
        /// Custom splash screen subtitle
        /// </summary>
        [StringLength(200)]
        public string? SplashSubtitle { get; set; }

        /// <summary>
        /// Custom button text for the splash screen
        /// </summary>
        [StringLength(50)]
        public string? SplashButtonText { get; set; }

        /// <summary>
        /// Background color for the splash screen (hex color)
        /// </summary>
        [StringLength(7)]
        public string? SplashBackgroundColor { get; set; }

        /// <summary>
        /// Text color for the splash screen (hex color)
        /// </summary>
        [StringLength(7)]
        public string? SplashTextColor { get; set; }

        /// <summary>
        /// Animation type for channel activation
        /// </summary>
        [StringLength(20)]
        public string AnimationType { get; set; } = "default";

        /// <summary>
        /// Animation duration in milliseconds
        /// </summary>
        public int AnimationDuration { get; set; } = 900;

        /// <summary>
        /// Sound effect to play on channel activation
        /// </summary>
        [StringLength(200)]
        public string? SoundEffect { get; set; }

        /// <summary>
        /// Whether to show the default splash bar
        /// </summary>
        public bool ShowSplashBar { get; set; } = true;

        /// <summary>
        /// Custom splash bar HTML content
        /// </summary>
        public string? CustomSplashBar { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created the channel
        /// </summary>
        [StringLength(100)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User who last updated the channel
        /// </summary>
        [StringLength(100)]
        public string? UpdatedBy { get; set; }
    }
}