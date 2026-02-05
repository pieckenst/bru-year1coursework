using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Controller for managing Wii-style channels
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WiiChannelController : ControllerBase
    {
        private readonly IWiiChannelService _channelService;
        private readonly ILogger<WiiChannelController> _logger;

        public WiiChannelController(IWiiChannelService channelService, ILogger<WiiChannelController> logger)
        {
            _channelService = channelService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active channels for the frontend
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WiiChannel>>> GetActiveChannels()
        {
            try
            {
                var channels = await _channelService.GetActiveChannelsAsync();
                return Ok(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active channels");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get channel configuration as JSON for frontend
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> GetChannelConfiguration()
        {
            try
            {
                var config = await _channelService.GetChannelConfigurationJsonAsync();
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channel configuration");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all channels (admin only)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WiiChannel>>> GetAllChannels()
        {
            try
            {
                var channels = await _channelService.GetAllChannelsAsync();
                return Ok(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all channels");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get channel by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<WiiChannel>> GetChannel(int id)
        {
            try
            {
                var channel = await _channelService.GetChannelByIdAsync(id);
                if (channel == null)
                    return NotFound();

                return Ok(channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channel {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get channel by key
        /// </summary>
        [HttpGet("key/{channelKey}")]
        [AllowAnonymous]
        public async Task<ActionResult<WiiChannel>> GetChannelByKey(string channelKey)
        {
            try
            {
                var channel = await _channelService.GetChannelByKeyAsync(channelKey);
                if (channel == null)
                    return NotFound();

                return Ok(channel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channel by key {Key}", channelKey);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new channel
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WiiChannel>> CreateChannel([FromBody] CreateChannelRequest request)
        {
            try
            {
                var channel = new WiiChannel
                {
                    ChannelKey = request.ChannelKey,
                    Name = request.Name,
                    Description = request.Description,
                    Position = request.Position,
                    IsActive = request.IsActive,
                    IconPath = request.IconPath,
                    SplashImagePath = request.SplashImagePath,
                    SplashHtmlContent = request.SplashHtmlContent,
                    SplashCssClasses = request.SplashCssClasses,
                    ActionUrl = request.ActionUrl,
                    ActionType = request.ActionType,
                    CustomJavaScript = request.CustomJavaScript,
                    ConfigurationJson = request.ConfigurationJson,
                    SortOrder = request.SortOrder,
                    Category = request.Category,
                    SplashTitle = request.SplashTitle,
                    SplashSubtitle = request.SplashSubtitle,
                    SplashButtonText = request.SplashButtonText,
                    SplashBackgroundColor = request.SplashBackgroundColor,
                    SplashTextColor = request.SplashTextColor,
                    AnimationType = request.AnimationType,
                    AnimationDuration = request.AnimationDuration,
                    SoundEffect = request.SoundEffect,
                    ShowSplashBar = request.ShowSplashBar,
                    CustomSplashBar = request.CustomSplashBar,
                    CreatedBy = User.Identity?.Name
                };

                var validation = await _channelService.ValidateChannelAsync(channel);
                if (!validation.IsValid)
                {
                    return BadRequest(new { errors = validation.Errors });
                }

                var createdChannel = await _channelService.CreateChannelAsync(channel);
                return CreatedAtAction(nameof(GetChannel), new { id = createdChannel.Id }, createdChannel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating channel");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing channel
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<WiiChannel>> UpdateChannel(int id, [FromBody] UpdateChannelRequest request)
        {
            try
            {
                var existingChannel = await _channelService.GetChannelByIdAsync(id);
                if (existingChannel == null)
                    return NotFound();

                // Update properties
                existingChannel.ChannelKey = request.ChannelKey;
                existingChannel.Name = request.Name;
                existingChannel.Description = request.Description;
                existingChannel.Position = request.Position;
                existingChannel.IsActive = request.IsActive;
                existingChannel.IconPath = request.IconPath;
                existingChannel.SplashImagePath = request.SplashImagePath;
                existingChannel.SplashHtmlContent = request.SplashHtmlContent;
                existingChannel.SplashCssClasses = request.SplashCssClasses;
                existingChannel.ActionUrl = request.ActionUrl;
                existingChannel.ActionType = request.ActionType;
                existingChannel.CustomJavaScript = request.CustomJavaScript;
                existingChannel.ConfigurationJson = request.ConfigurationJson;
                existingChannel.SortOrder = request.SortOrder;
                existingChannel.Category = request.Category;
                existingChannel.SplashTitle = request.SplashTitle;
                existingChannel.SplashSubtitle = request.SplashSubtitle;
                existingChannel.SplashButtonText = request.SplashButtonText;
                existingChannel.SplashBackgroundColor = request.SplashBackgroundColor;
                existingChannel.SplashTextColor = request.SplashTextColor;
                existingChannel.AnimationType = request.AnimationType;
                existingChannel.AnimationDuration = request.AnimationDuration;
                existingChannel.SoundEffect = request.SoundEffect;
                existingChannel.ShowSplashBar = request.ShowSplashBar;
                existingChannel.CustomSplashBar = request.CustomSplashBar;
                existingChannel.UpdatedBy = User.Identity?.Name;

                var validation = await _channelService.ValidateChannelAsync(existingChannel);
                if (!validation.IsValid)
                {
                    return BadRequest(new { errors = validation.Errors });
                }

                var updatedChannel = await _channelService.UpdateChannelAsync(existingChannel);
                return Ok(updatedChannel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a channel
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteChannel(int id)
        {
            try
            {
                var success = await _channelService.DeleteChannelAsync(id);
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting channel {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get channels by category
        /// </summary>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<WiiChannel>>> GetChannelsByCategory(string category)
        {
            try
            {
                var channels = await _channelService.GetChannelsByCategoryAsync(category);
                return Ok(channels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channels by category {Category}", category);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Import channels from JSON
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult> ImportChannels([FromBody] ImportChannelsRequest request)
        {
            try
            {
                var success = await _channelService.ImportChannelsFromJsonAsync(request.JsonConfiguration);
                if (!success)
                    return BadRequest("Failed to import channels");

                return Ok(new { message = "Channels imported successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing channels");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Reset to default channels
        /// </summary>
        [HttpPost("reset")]
        public async Task<ActionResult> ResetToDefault()
        {
            try
            {
                var success = await _channelService.ResetToDefaultChannelsAsync();
                if (!success)
                    return BadRequest("Failed to reset channels");

                return Ok(new { message = "Channels reset to default successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting channels");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get available positions
        /// </summary>
        [HttpGet("positions/available")]
        public async Task<ActionResult<List<int>>> GetAvailablePositions()
        {
            try
            {
                var positions = await _channelService.GetAvailablePositionsAsync();
                return Ok(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available positions");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Reorder channels
        /// </summary>
        [HttpPost("reorder")]
        public async Task<ActionResult> ReorderChannels([FromBody] ReorderChannelsRequest request)
        {
            try
            {
                var success = await _channelService.ReorderChannelsAsync(request.ChannelPositions);
                if (!success)
                    return BadRequest("Failed to reorder channels");

                return Ok(new { message = "Channels reordered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering channels");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    // Request DTOs
    public class CreateChannelRequest
    {
        [Required]
        [StringLength(50)]
        public string ChannelKey { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(1, 12)]
        public int Position { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? IconPath { get; set; }

        [StringLength(500)]
        public string? SplashImagePath { get; set; }

        public string? SplashHtmlContent { get; set; }

        [StringLength(200)]
        public string? SplashCssClasses { get; set; }

        [StringLength(500)]
        public string? ActionUrl { get; set; }

        [StringLength(20)]
        public string ActionType { get; set; } = "url";

        public string? CustomJavaScript { get; set; }

        public string? ConfigurationJson { get; set; }

        public int SortOrder { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(100)]
        public string? SplashTitle { get; set; }

        [StringLength(200)]
        public string? SplashSubtitle { get; set; }

        [StringLength(50)]
        public string? SplashButtonText { get; set; }

        [StringLength(7)]
        public string? SplashBackgroundColor { get; set; }

        [StringLength(7)]
        public string? SplashTextColor { get; set; }

        [StringLength(20)]
        public string AnimationType { get; set; } = "default";

        public int AnimationDuration { get; set; } = 900;

        [StringLength(200)]
        public string? SoundEffect { get; set; }

        public bool ShowSplashBar { get; set; } = true;

        public string? CustomSplashBar { get; set; }
    }

    public class UpdateChannelRequest : CreateChannelRequest
    {
        // Inherits all properties from CreateChannelRequest
    }

    public class ImportChannelsRequest
    {
        [Required]
        public string JsonConfiguration { get; set; } = string.Empty;
    }

    public class ReorderChannelsRequest
    {
        [Required]
        public Dictionary<int, int> ChannelPositions { get; set; } = new();
    }
}