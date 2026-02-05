using TicketSalesApp.Core.Models;

namespace TicketSalesApp.Services.Interfaces
{
    /// <summary>
    /// Service interface for managing Wii-style channels
    /// </summary>
    public interface IWiiChannelService
    {
        /// <summary>
        /// Get all active channels ordered by position
        /// </summary>
        Task<IEnumerable<WiiChannel>> GetActiveChannelsAsync();

        /// <summary>
        /// Get all channels (including inactive)
        /// </summary>
        Task<IEnumerable<WiiChannel>> GetAllChannelsAsync();

        /// <summary>
        /// Get channel by ID
        /// </summary>
        Task<WiiChannel?> GetChannelByIdAsync(int id);

        /// <summary>
        /// Get channel by key
        /// </summary>
        Task<WiiChannel?> GetChannelByKeyAsync(string channelKey);

        /// <summary>
        /// Create a new channel
        /// </summary>
        Task<WiiChannel> CreateChannelAsync(WiiChannel channel);

        /// <summary>
        /// Update an existing channel
        /// </summary>
        Task<WiiChannel> UpdateChannelAsync(WiiChannel channel);

        /// <summary>
        /// Delete a channel
        /// </summary>
        Task<bool> DeleteChannelAsync(int id);

        /// <summary>
        /// Get channels by category
        /// </summary>
        Task<IEnumerable<WiiChannel>> GetChannelsByCategoryAsync(string category);

        /// <summary>
        /// Get channel configuration as JSON for frontend
        /// </summary>
        Task<string> GetChannelConfigurationJsonAsync();

        /// <summary>
        /// Import channels from JSON configuration
        /// </summary>
        Task<bool> ImportChannelsFromJsonAsync(string jsonConfiguration);

        /// <summary>
        /// Reset channels to default configuration
        /// </summary>
        Task<bool> ResetToDefaultChannelsAsync();

        /// <summary>
        /// Validate channel configuration
        /// </summary>
        Task<(bool IsValid, List<string> Errors)> ValidateChannelAsync(WiiChannel channel);

        /// <summary>
        /// Get available positions for new channels
        /// </summary>
        Task<List<int>> GetAvailablePositionsAsync();

        /// <summary>
        /// Reorder channels
        /// </summary>
        Task<bool> ReorderChannelsAsync(Dictionary<int, int> channelPositions);
    }
}