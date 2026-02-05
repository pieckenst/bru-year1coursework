using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Service for managing Wii-style channels
    /// </summary>
    public class WiiChannelService : IWiiChannelService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WiiChannelService> _logger;

        public WiiChannelService(AppDbContext context, ILogger<WiiChannelService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<WiiChannel>> GetActiveChannelsAsync()
        {
            _logger.LogInformation("Getting active channels from database");
            
            try
            {
                var startTime = DateTime.UtcNow;
                var channels = await _context.WiiChannels
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Position)
                    .ThenBy(c => c.SortOrder)
                    .ToListAsync();
                
                var queryTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Retrieved {Count} active channels in {QueryTime}ms", 
                    channels.Count(), queryTime.TotalMilliseconds);
                
                foreach (var channel in channels)
                {
                    _logger.LogDebug("Active channel: {ChannelKey} - Name: {Name}, Position: {Position}, SpriteId: {SpriteId}, IconPath: {IconPath}", 
                        channel.ChannelKey, channel.Name, channel.Position, channel.SpriteId, channel.IconPath);
                }
                
                return channels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active channels from database");
                return new List<WiiChannel>();
            }
        }

        public async Task<IEnumerable<WiiChannel>> GetAllChannelsAsync()
        {
            _logger.LogInformation("Getting all channels from database");
            
            try
            {
                var startTime = DateTime.UtcNow;
                var channels = await _context.WiiChannels
                    .OrderBy(c => c.Position)
                    .ThenBy(c => c.SortOrder)
                    .ToListAsync();
                
                var queryTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Retrieved {Count} total channels in {QueryTime}ms", 
                    channels.Count(), queryTime.TotalMilliseconds);
                
                var activeCount = channels.Count(c => c.IsActive);
                var inactiveCount = channels.Count(c => !c.IsActive);
                _logger.LogDebug("Channel breakdown: {ActiveCount} active, {InactiveCount} inactive", 
                    activeCount, inactiveCount);
                
                return channels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all channels from database");
                return new List<WiiChannel>();
            }
        }

        public async Task<WiiChannel?> GetChannelByIdAsync(int id)
        {
            _logger.LogInformation("Getting channel by ID: {Id}", id);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var channel = await _context.WiiChannels.FindAsync(id);
                var queryTime = DateTime.UtcNow - startTime;
                
                if (channel != null)
                {
                    _logger.LogInformation("Found channel by ID {Id}: {ChannelKey} - {Name} in {QueryTime}ms", 
                        id, channel.ChannelKey, channel.Name, queryTime.TotalMilliseconds);
                    _logger.LogDebug("Channel details: Position: {Position}, SpriteId: {SpriteId}, IconPath: {IconPath}, IsActive: {IsActive}", 
                        channel.Position, channel.SpriteId, channel.IconPath, channel.IsActive);
                }
                else
                {
                    _logger.LogWarning("Channel not found by ID: {Id} (query took {QueryTime}ms)", 
                        id, queryTime.TotalMilliseconds);
                }
                
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channel by ID: {Id}", id);
                return null;
            }
        }

        public async Task<WiiChannel?> GetChannelByKeyAsync(string channelKey)
        {
            _logger.LogInformation("Getting channel by key: {ChannelKey}", channelKey);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var channel = await _context.WiiChannels
                    .FirstOrDefaultAsync(c => c.ChannelKey == channelKey);
                var queryTime = DateTime.UtcNow - startTime;
                
                if (channel != null)
                {
                    _logger.LogInformation("Found channel by key {ChannelKey}: {Name} (ID: {Id}) in {QueryTime}ms", 
                        channelKey, channel.Name, channel.Id, queryTime.TotalMilliseconds);
                    _logger.LogDebug("Channel details: Position: {Position}, SpriteId: {SpriteId}, IconPath: {IconPath}, ActionType: {ActionType}, IsActive: {IsActive}", 
                        channel.Position, channel.SpriteId, channel.IconPath, channel.ActionType, channel.IsActive);
                }
                else
                {
                    _logger.LogWarning("Channel not found by key: {ChannelKey} (query took {QueryTime}ms)", 
                        channelKey, queryTime.TotalMilliseconds);
                }
                
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channel by key: {Key}", channelKey);
                return null;
            }
        }

        public async Task<WiiChannel> CreateChannelAsync(WiiChannel channel)
        {
            _logger.LogInformation("Creating new channel: {ChannelKey} - {Name}", channel.ChannelKey, channel.Name);
            
            try
            {
                channel.CreatedAt = DateTime.UtcNow;
                channel.UpdatedAt = DateTime.UtcNow;

                _logger.LogDebug("Channel creation details: Position: {Position}, SpriteId: {SpriteId}, IconPath: {IconPath}, ActionType: {ActionType}, IsActive: {IsActive}", 
                    channel.Position, channel.SpriteId, channel.IconPath, channel.ActionType, channel.IsActive);

                var startTime = DateTime.UtcNow;
                _context.WiiChannels.Add(channel);
                await _context.SaveChangesAsync();
                var saveTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Successfully created channel: {ChannelKey} (ID: {Id}) in {SaveTime}ms", 
                    channel.ChannelKey, channel.Id, saveTime.TotalMilliseconds);
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating channel: {ChannelKey} - {Name}", channel.ChannelKey, channel.Name);
                throw;
            }
        }

        public async Task<WiiChannel> UpdateChannelAsync(WiiChannel channel)
        {
            _logger.LogInformation("Updating channel: {ChannelKey} (ID: {Id}) - {Name}", channel.ChannelKey, channel.Id, channel.Name);
            
            try
            {
                channel.UpdatedAt = DateTime.UtcNow;

                _logger.LogDebug("Channel update details: Position: {Position}, SpriteId: {SpriteId}, IconPath: {IconPath}, ActionType: {ActionType}, IsActive: {IsActive}", 
                    channel.Position, channel.SpriteId, channel.IconPath, channel.ActionType, channel.IsActive);

                var startTime = DateTime.UtcNow;
                _context.WiiChannels.Update(channel);
                await _context.SaveChangesAsync();
                var saveTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Successfully updated channel: {ChannelKey} (ID: {Id}) in {SaveTime}ms", 
                    channel.ChannelKey, channel.Id, saveTime.TotalMilliseconds);
                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel: {ChannelKey} (ID: {Id})", channel.ChannelKey, channel.Id);
                throw;
            }
        }

        public async Task<bool> DeleteChannelAsync(int id)
        {
            _logger.LogInformation("Deleting channel with ID: {Id}", id);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var channel = await _context.WiiChannels.FindAsync(id);
                var findTime = DateTime.UtcNow - startTime;
                
                if (channel == null)
                {
                    _logger.LogWarning("Channel not found for deletion: ID {Id} (search took {FindTime}ms)", 
                        id, findTime.TotalMilliseconds);
                    return false;
                }

                _logger.LogDebug("Found channel for deletion: {ChannelKey} - {Name} at position {Position}", 
                    channel.ChannelKey, channel.Name, channel.Position);

                var deleteStartTime = DateTime.UtcNow;
                _context.WiiChannels.Remove(channel);
                await _context.SaveChangesAsync();
                var deleteTime = DateTime.UtcNow - deleteStartTime;

                _logger.LogInformation("Successfully deleted channel: {ChannelKey} (ID: {Id}) in {DeleteTime}ms", 
                    channel.ChannelKey, id, deleteTime.TotalMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting channel with ID: {Id}", id);
                return false;
            }
        }

        public async Task<IEnumerable<WiiChannel>> GetChannelsByCategoryAsync(string category)
        {
            _logger.LogInformation("Getting channels by category: {Category}", category);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var channels = await _context.WiiChannels
                    .Where(c => c.Category == category && c.IsActive)
                    .OrderBy(c => c.Position)
                    .ThenBy(c => c.SortOrder)
                    .ToListAsync();
                
                var queryTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Retrieved {Count} channels in category '{Category}' in {QueryTime}ms", 
                    channels.Count(), category, queryTime.TotalMilliseconds);
                
                foreach (var channel in channels)
                {
                    _logger.LogDebug("Category channel: {ChannelKey} - {Name} at position {Position}", 
                        channel.ChannelKey, channel.Name, channel.Position);
                }
                
                return channels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving channels by category: {Category}", category);
                return new List<WiiChannel>();
            }
        }

        public async Task<string> GetChannelConfigurationJsonAsync()
        {
            _logger.LogInformation("Generating channel configuration JSON");
            
            try
            {
                var startTime = DateTime.UtcNow;
                var channels = await GetActiveChannelsAsync();
                var channelsList = channels.ToList();
                
                _logger.LogDebug("Retrieved {Count} active channels for JSON configuration", channelsList.Count);
                
                var config = new
                {
                    channels = channelsList.Select(c => new
                    {
                        id = c.Id,
                        channelKey = c.ChannelKey,
                        name = c.Name,
                        description = c.Description,
                        position = c.Position,
                        spriteId = c.SpriteId,
                        iconPath = c.IconPath,
                        splashImagePath = c.SplashImagePath,
                        splashHtmlContent = c.SplashHtmlContent,
                        splashCssClasses = c.SplashCssClasses,
                        actionUrl = c.ActionUrl,
                        actionType = c.ActionType,
                        customJavaScript = c.CustomJavaScript,
                        splashTitle = c.SplashTitle,
                        splashSubtitle = c.SplashSubtitle,
                        splashButtonText = c.SplashButtonText,
                        splashBackgroundColor = c.SplashBackgroundColor,
                        splashTextColor = c.SplashTextColor,
                        animationType = c.AnimationType,
                        animationDuration = c.AnimationDuration,
                        soundEffect = c.SoundEffect,
                        showSplashBar = c.ShowSplashBar,
                        customSplashBar = c.CustomSplashBar,
                        category = c.Category,
                        sortOrder = c.SortOrder,
                        configurationJson = c.ConfigurationJson
                    }),
                    lastUpdated = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
                
                var generationTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Generated channel configuration JSON in {GenerationTime}ms - Size: {Size} bytes, Channels: {Count}", 
                    generationTime.TotalMilliseconds, json.Length, channelsList.Count);
                
                foreach (var channel in channelsList)
                {
                    _logger.LogDebug("JSON config includes channel: {ChannelKey} - {Name} at position {Position}", 
                        channel.ChannelKey, channel.Name, channel.Position);
                }
                
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating channel configuration JSON");
                return "{}";
            }
        }

        public async Task<bool> ImportChannelsFromJsonAsync(string jsonConfiguration)
        {
            _logger.LogInformation("Importing channels from JSON configuration - Size: {Size} bytes", jsonConfiguration.Length);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var config = JsonSerializer.Deserialize<JsonElement>(jsonConfiguration);
                
                if (!config.TryGetProperty("channels", out var channelsElement))
                {
                    _logger.LogWarning("JSON configuration missing 'channels' property");
                    return false;
                }

                var channels = new List<WiiChannel>();
                var channelCount = 0;
                
                foreach (var channelElement in channelsElement.EnumerateArray())
                {
                    channelCount++;
                    var channelKey = channelElement.GetProperty("channelKey").GetString() ?? "";
                    
                    _logger.LogDebug("Processing channel {Index}: {ChannelKey}", channelCount, channelKey);
                    
                    var channel = new WiiChannel
                    {
                        ChannelKey = channelKey,
                        Name = channelElement.GetProperty("name").GetString() ?? "",
                        Description = channelElement.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                        Position = channelElement.GetProperty("position").GetInt32(),
                        IsActive = channelElement.TryGetProperty("isActive", out var active) ? active.GetBoolean() : true,
                        SpriteId = channelElement.TryGetProperty("spriteId", out var spriteId) ? spriteId.GetString() : null,
                        IconPath = channelElement.TryGetProperty("iconPath", out var icon) ? icon.GetString() : null,
                        SplashImagePath = channelElement.TryGetProperty("splashImagePath", out var splash) ? splash.GetString() : null,
                        SplashHtmlContent = channelElement.TryGetProperty("splashHtmlContent", out var html) ? html.GetString() : null,
                        SplashCssClasses = channelElement.TryGetProperty("splashCssClasses", out var css) ? css.GetString() : null,
                        ActionUrl = channelElement.TryGetProperty("actionUrl", out var url) ? url.GetString() : null,
                        ActionType = channelElement.TryGetProperty("actionType", out var type) ? type.GetString() ?? "url" : "url",
                        CustomJavaScript = channelElement.TryGetProperty("customJavaScript", out var js) ? js.GetString() : null,
                        SplashTitle = channelElement.TryGetProperty("splashTitle", out var title) ? title.GetString() : null,
                        SplashSubtitle = channelElement.TryGetProperty("splashSubtitle", out var subtitle) ? subtitle.GetString() : null,
                        SplashButtonText = channelElement.TryGetProperty("splashButtonText", out var btnText) ? btnText.GetString() : null,
                        SplashBackgroundColor = channelElement.TryGetProperty("splashBackgroundColor", out var bgColor) ? bgColor.GetString() : null,
                        SplashTextColor = channelElement.TryGetProperty("splashTextColor", out var textColor) ? textColor.GetString() : null,
                        AnimationType = channelElement.TryGetProperty("animationType", out var animType) ? animType.GetString() ?? "default" : "default",
                        AnimationDuration = channelElement.TryGetProperty("animationDuration", out var animDur) ? animDur.GetInt32() : 900,
                        SoundEffect = channelElement.TryGetProperty("soundEffect", out var sound) ? sound.GetString() : null,
                        ShowSplashBar = channelElement.TryGetProperty("showSplashBar", out var showBar) ? showBar.GetBoolean() : true,
                        CustomSplashBar = channelElement.TryGetProperty("customSplashBar", out var customBar) ? customBar.GetString() : null,
                        Category = channelElement.TryGetProperty("category", out var cat) ? cat.GetString() : null,
                        SortOrder = channelElement.TryGetProperty("sortOrder", out var sort) ? sort.GetInt32() : 0,
                        ConfigurationJson = channelElement.TryGetProperty("configurationJson", out var configJson) ? configJson.GetString() : null
                    };

                    channels.Add(channel);
                    _logger.LogDebug("Imported channel: {ChannelKey} - {Name} at position {Position}", 
                        channel.ChannelKey, channel.Name, channel.Position);
                }

                // Clear existing channels and add new ones
                var existingChannels = await _context.WiiChannels.ToListAsync();
                _logger.LogInformation("Removing {Count} existing channels before import", existingChannels.Count);
                
                _context.WiiChannels.RemoveRange(existingChannels);
                _context.WiiChannels.AddRange(channels);
                
                var saveStartTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                var saveTime = DateTime.UtcNow - saveStartTime;
                var totalTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Successfully imported {Count} channels from JSON in {TotalTime}ms (save: {SaveTime}ms)", 
                    channels.Count, totalTime.TotalMilliseconds, saveTime.TotalMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing channels from JSON configuration");
                return false;
            }
        }

        public async Task<bool> ResetToDefaultChannelsAsync()
        {
            _logger.LogInformation("Resetting to default channels");
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Clear existing channels
                var existingChannels = await _context.WiiChannels.ToListAsync();
                _logger.LogInformation("Removing {Count} existing channels", existingChannels.Count);
                _context.WiiChannels.RemoveRange(existingChannels);

                // Add default channels
                var defaultChannels = GetDefaultChannels();
                _logger.LogInformation("Adding {Count} default channels", defaultChannels.Count);
                
                foreach (var channel in defaultChannels)
                {
                    _logger.LogDebug("Adding default channel: {ChannelKey} - {Name} at position {Position}", 
                        channel.ChannelKey, channel.Name, channel.Position);
                }
                
                _context.WiiChannels.AddRange(defaultChannels);
                
                var saveStartTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                var saveTime = DateTime.UtcNow - saveStartTime;
                var totalTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Successfully reset to {Count} default channels in {TotalTime}ms (save: {SaveTime}ms)", 
                    defaultChannels.Count, totalTime.TotalMilliseconds, saveTime.TotalMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting to default channels");
                return false;
            }
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateChannelAsync(WiiChannel channel)
        {
            _logger.LogInformation("Validating channel: {ChannelKey} - {Name}", channel.ChannelKey, channel.Name);
            
            var errors = new List<string>();

            // Check required fields
            if (string.IsNullOrWhiteSpace(channel.ChannelKey))
            {
                errors.Add("Channel key is required");
                _logger.LogDebug("Validation error: Channel key is required");
            }

            if (string.IsNullOrWhiteSpace(channel.Name))
            {
                errors.Add("Channel name is required");
                _logger.LogDebug("Validation error: Channel name is required");
            }

            if (channel.Position < 1 || channel.Position > 12)
            {
                errors.Add("Position must be between 1 and 12");
                _logger.LogDebug("Validation error: Position {Position} is out of range (1-12)", channel.Position);
            }

            // Check for duplicate channel key
            var existingChannel = await _context.WiiChannels
                .FirstOrDefaultAsync(c => c.ChannelKey == channel.ChannelKey && c.Id != channel.Id);
            
            if (existingChannel != null)
            {
                errors.Add("Channel key must be unique");
                _logger.LogDebug("Validation error: Duplicate channel key {ChannelKey} found (existing ID: {ExistingId})", 
                    channel.ChannelKey, existingChannel.Id);
            }

            // Check for duplicate position
            var existingPosition = await _context.WiiChannels
                .FirstOrDefaultAsync(c => c.Position == channel.Position && c.Id != channel.Id && c.IsActive);
            
            if (existingPosition != null)
            {
                errors.Add("Position is already occupied by another active channel");
                _logger.LogDebug("Validation error: Position {Position} is occupied by channel {ExistingChannelKey} (ID: {ExistingId})", 
                    channel.Position, existingPosition.ChannelKey, existingPosition.Id);
            }

            // Validate action type
            var validActionTypes = new[] { "url", "view", "javascript", "api" };
            if (!validActionTypes.Contains(channel.ActionType))
            {
                errors.Add("Invalid action type");
                _logger.LogDebug("Validation error: Invalid action type {ActionType}, valid types: {ValidTypes}", 
                    channel.ActionType, string.Join(", ", validActionTypes));
            }

            // Validate animation type
            var validAnimationTypes = new[] { "default", "zoom", "fade", "slide", "bounce" };
            if (!validAnimationTypes.Contains(channel.AnimationType))
            {
                errors.Add("Invalid animation type");
                _logger.LogDebug("Validation error: Invalid animation type {AnimationType}, valid types: {ValidTypes}", 
                    channel.AnimationType, string.Join(", ", validAnimationTypes));
            }

            var isValid = errors.Count == 0;
            _logger.LogInformation("Channel validation completed: {ChannelKey} - Valid: {IsValid}, Errors: {ErrorCount}", 
                channel.ChannelKey, isValid, errors.Count);
            
            if (!isValid)
            {
                _logger.LogDebug("Validation errors for {ChannelKey}: {Errors}", 
                    channel.ChannelKey, string.Join("; ", errors));
            }

            return (isValid, errors);
        }

        public async Task<List<int>> GetAvailablePositionsAsync()
        {
            _logger.LogInformation("Getting available channel positions");
            
            try
            {
                var startTime = DateTime.UtcNow;
                var occupiedPositions = await _context.WiiChannels
                    .Where(c => c.IsActive)
                    .Select(c => c.Position)
                    .ToListAsync();
                
                var queryTime = DateTime.UtcNow - startTime;
                var allPositions = Enumerable.Range(1, 12).ToList();
                var availablePositions = allPositions.Except(occupiedPositions).ToList();
                
                _logger.LogInformation("Found {AvailableCount} available positions out of 12 total in {QueryTime}ms", 
                    availablePositions.Count, queryTime.TotalMilliseconds);
                _logger.LogDebug("Occupied positions: {OccupiedPositions}", string.Join(", ", occupiedPositions.OrderBy(p => p)));
                _logger.LogDebug("Available positions: {AvailablePositions}", string.Join(", ", availablePositions.OrderBy(p => p)));
                
                return availablePositions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available positions");
                return new List<int>();
            }
        }

        public async Task<bool> ReorderChannelsAsync(Dictionary<int, int> channelPositions)
        {
            _logger.LogInformation("Reordering {Count} channels", channelPositions.Count);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var updatedChannels = new List<string>();
                
                foreach (var kvp in channelPositions)
                {
                    var channel = await _context.WiiChannels.FindAsync(kvp.Key);
                    if (channel != null)
                    {
                        var oldPosition = channel.Position;
                        channel.Position = kvp.Value;
                        channel.UpdatedAt = DateTime.UtcNow;
                        
                        updatedChannels.Add($"{channel.ChannelKey}: {oldPosition} → {kvp.Value}");
                        _logger.LogDebug("Reordered channel {ChannelKey} (ID: {Id}): position {OldPosition} → {NewPosition}", 
                            channel.ChannelKey, kvp.Key, oldPosition, kvp.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Channel not found for reordering: ID {Id}", kvp.Key);
                    }
                }

                var saveStartTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                var saveTime = DateTime.UtcNow - saveStartTime;
                var totalTime = DateTime.UtcNow - startTime;
                
                _logger.LogInformation("Successfully reordered {Count} channels in {TotalTime}ms (save: {SaveTime}ms)", 
                    updatedChannels.Count, totalTime.TotalMilliseconds, saveTime.TotalMilliseconds);
                _logger.LogDebug("Channel reordering details: {Updates}", string.Join("; ", updatedChannels));
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering channels");
                return false;
            }
        }

        private List<WiiChannel> GetDefaultChannels()
        {
            _logger.LogDebug("Creating default channel configuration");
            
            var defaultChannels = new List<WiiChannel>
            {
                new WiiChannel
                {
                    ChannelKey = "mii-channel",
                    Name = "Mii Channel",
                    Description = "Create and customize your Mii characters",
                    Position = 1,
                    SpriteId = "wiilogo", // Uses channel-wiilogo.png sprite
                    IconPath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashImagePath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashTitle = "Mii Channel",
                    SplashSubtitle = "Create and customize your Mii",
                    SplashButtonText = "Start",
                    ActionType = "javascript",
                    CustomJavaScript = "console.log('Mii Channel activated - using sprite system with authentic Wii experience');",
                    Category = "system",
                    SortOrder = 1,
                    IsActive = true,
                    AnimationType = "default",
                    AnimationDuration = 900,
                    ShowSplashBar = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new WiiChannel
                {
                    ChannelKey = "authentication",
                    Name = "Authentication",
                    Description = "Sign in and manage your account",
                    Position = 2,
                    SpriteId = null, // Use IconPath directly for SVG support
                    IconPath = "/customerui/assets/images/channel-auth-icon.svg",
                    SplashImagePath = "/customerui/assets/images/channel-auth-banner.svg",
                    SplashTitle = "Authentication",
                    SplashSubtitle = "Sign in to your account",
                    SplashButtonText = "Continue",
                    ActionType = "view",
                    ActionUrl = "auth-channel",
                    Category = "system",
                    SortOrder = 2,
                    IsActive = true,
                    AnimationType = "default",
                    AnimationDuration = 900,
                    ShowSplashBar = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new WiiChannel
                {
                    ChannelKey = "ticket-sales",
                    Name = "Ticket Sales",
                    Description = "Purchase bus tickets and manage your bookings",
                    Position = 3,
                    SpriteId = "wiilogo", // Uses channel-wiilogo.png sprite
                    IconPath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashImagePath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashTitle = "Ticket Sales",
                    SplashSubtitle = "Purchase your bus tickets",
                    SplashButtonText = "Buy Tickets",
                    ActionType = "view",
                    ActionUrl = "ticket-purchase",
                    Category = "main",
                    SortOrder = 3,
                    IsActive = true,
                    AnimationType = "default",
                    AnimationDuration = 900,
                    ShowSplashBar = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new WiiChannel
                {
                    ChannelKey = "schedule",
                    Name = "Bus Schedule",
                    Description = "View bus schedules and route information",
                    Position = 4,
                    SpriteId = "wiilogo", // Uses channel-wiilogo.png sprite
                    IconPath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashImagePath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashTitle = "Bus Schedule",
                    SplashSubtitle = "Check departure times and routes",
                    SplashButtonText = "View Schedule",
                    ActionType = "view",
                    ActionUrl = "schedule",
                    Category = "main",
                    SortOrder = 3,
                    IsActive = true,
                    AnimationType = "default",
                    AnimationDuration = 900,
                    ShowSplashBar = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new WiiChannel
                {
                    ChannelKey = "my-tickets",
                    Name = "My Tickets",
                    Description = "View and manage your purchased tickets",
                    Position = 7,
                    SpriteId = "wiilogo", // Uses channel-wiilogo.png sprite
                    IconPath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashImagePath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashTitle = "My Tickets",
                    SplashSubtitle = "Manage your bookings",
                    SplashButtonText = "View Tickets",
                    ActionType = "view",
                    ActionUrl = "my-tickets",
                    Category = "main",
                    SortOrder = 4,
                    IsActive = true,
                    AnimationType = "default",
                    AnimationDuration = 900,
                    ShowSplashBar = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new WiiChannel
                {
                    ChannelKey = "settings",
                    Name = "Settings",
                    Description = "System settings and preferences",
                    Position = 10,
                    SpriteId = "wiilogo", // Uses channel-wiilogo.png sprite
                    IconPath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashImagePath = "/customerui/assets/images/channel-wiilogo.png",
                    SplashTitle = "Settings",
                    SplashSubtitle = "Configure system preferences",
                    SplashButtonText = "Open Settings",
                    ActionType = "view",
                    ActionUrl = "settings-main",
                    Category = "system",
                    SortOrder = 5,
                    IsActive = true,
                    AnimationType = "default",
                    AnimationDuration = 900,
                    ShowSplashBar = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            };

            _logger.LogDebug("Created {Count} default channels with sprite system support", defaultChannels.Count);
            foreach (var channel in defaultChannels)
            {
                _logger.LogDebug("Default channel: {ChannelKey} - {Name} at position {Position} (sprite: {SpriteId})", 
                    channel.ChannelKey, channel.Name, channel.Position, channel.SpriteId);
            }

            return defaultChannels;
        }
    }
}