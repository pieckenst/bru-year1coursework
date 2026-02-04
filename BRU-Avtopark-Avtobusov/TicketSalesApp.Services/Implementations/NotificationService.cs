using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations;

/// <summary>
/// Service for managing real-time notifications via SignalR WebSocket connections
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationHubContext _hubContext;
    private readonly ILogger<NotificationService> _logger;
    
    // Static connection tracking (shared across all instances)
    private static readonly ConcurrentDictionary<string, string> _userConnections = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _roleGroups = new();

    public NotificationService(INotificationHubContext hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Register a user connection (called from Hub)
    /// </summary>
    public static void RegisterConnection(string connectionId, string userId, int role)
    {
        _userConnections.TryAdd(connectionId, userId);
        
        var roleGroupName = $"Role_{role}";
        _roleGroups.AddOrUpdate(roleGroupName, 
            new HashSet<string> { connectionId },
            (key, existing) => { existing.Add(connectionId); return existing; });
    }

    /// <summary>
    /// Unregister a user connection (called from Hub)
    /// </summary>
    public static void UnregisterConnection(string connectionId, int role)
    {
        _userConnections.TryRemove(connectionId, out _);
        
        var roleGroupName = $"Role_{role}";
        if (_roleGroups.TryGetValue(roleGroupName, out var connections))
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _roleGroups.TryRemove(roleGroupName, out _);
            }
        }
    }

    /// <summary>
    /// Send notification to all connected clients
    /// </summary>
    public async Task SendToAllAsync(string message, object? data = null)
    {
        try
        {
            var notification = new
            {
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Type = "Broadcast"
            };

            await _hubContext.SendToAllAsync("Notification", notification);
            
            _logger.LogInformation("Broadcast notification sent: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send broadcast notification: {Message}", message);
            throw;
        }
    }

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    public async Task SendToUserAsync(long userId, string message, object? data = null)
    {
        try
        {
            var userGroupName = $"User_{userId}";
            var notification = new
            {
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Type = "UserSpecific",
                TargetUserId = userId
            };

            await _hubContext.SendToGroupAsync(userGroupName, "Notification", notification);
            
            _logger.LogInformation("User notification sent to {UserId}: {Message}", userId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send user notification to {UserId}: {Message}", userId, message);
            throw;
        }
    }

    /// <summary>
    /// Send notification to users with specific role
    /// </summary>
    public async Task SendToRoleAsync(int role, string message, object? data = null)
    {
        try
        {
            var roleGroupName = $"Role_{role}";
            var notification = new
            {
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Type = "RoleSpecific",
                TargetRole = role
            };

            await _hubContext.SendToGroupAsync(roleGroupName, "Notification", notification);
            
            _logger.LogInformation("Role notification sent to role {Role}: {Message}", role, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send role notification to role {Role}: {Message}", role, message);
            throw;
        }
    }

    /// <summary>
    /// Send notification to specific group
    /// </summary>
    public async Task SendToGroupAsync(string groupName, string message, object? data = null)
    {
        try
        {
            var notification = new
            {
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Type = "GroupSpecific",
                TargetGroup = groupName
            };

            await _hubContext.SendToGroupAsync(groupName, "Notification", notification);
            
            _logger.LogInformation("Group notification sent to {GroupName}: {Message}", groupName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send group notification to {GroupName}: {Message}", groupName, message);
            throw;
        }
    }

    /// <summary>
    /// Add user to a group
    /// </summary>
    public async Task AddToGroupAsync(string connectionId, string groupName)
    {
        try
        {
            await _hubContext.AddToGroupAsync(connectionId, groupName);
            
            _logger.LogDebug("Connection {ConnectionId} added to group {GroupName}", connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add connection {ConnectionId} to group {GroupName}", connectionId, groupName);
            throw;
        }
    }

    /// <summary>
    /// Remove user from a group
    /// </summary>
    public async Task RemoveFromGroupAsync(string connectionId, string groupName)
    {
        try
        {
            await _hubContext.RemoveFromGroupAsync(connectionId, groupName);
            
            _logger.LogDebug("Connection {ConnectionId} removed from group {GroupName}", connectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove connection {ConnectionId} from group {GroupName}", connectionId, groupName);
            throw;
        }
    }

    /// <summary>
    /// Broadcast data change notification
    /// </summary>
    public async Task BroadcastDataChangeAsync(string entityType, string action, long entityId, object? data = null)
    {
        try
        {
            var changeNotification = new
            {
                EntityType = entityType,
                Action = action, // Created, Updated, Deleted
                EntityId = entityId,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Type = "DataChange"
            };

            // Send to all authenticated users
            await _hubContext.SendToAllAsync("DataChange", changeNotification);
            
            _logger.LogInformation("Data change notification sent: {EntityType} {Action} (ID: {EntityId})", 
                entityType, action, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast data change: {EntityType} {Action} (ID: {EntityId})", 
                entityType, action, entityId);
            throw;
        }
    }

    /// <summary>
    /// Send export progress notification
    /// </summary>
    public async Task SendExportProgressAsync(long userId, string exportId, int progress, string status, string? message = null)
    {
        try
        {
            var userGroupName = $"User_{userId}";
            var progressNotification = new
            {
                ExportId = exportId,
                Progress = progress,
                Status = status,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Type = "ExportProgress"
            };

            await _hubContext.SendToGroupAsync(userGroupName, "ExportProgress", progressNotification);
            
            _logger.LogInformation("Export progress notification sent to user {UserId}: {ExportId} - {Progress}% ({Status})", 
                userId, exportId, progress, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send export progress to user {UserId}: {ExportId}", userId, exportId);
            throw;
        }
    }

    /// <summary>
    /// Get list of connected users
    /// </summary>
    public async Task<IEnumerable<string>> GetConnectedUsersAsync()
    {
        try
        {
            var connectedUsers = _userConnections.Values.Distinct();
            
            _logger.LogDebug("Retrieved {Count} connected users", connectedUsers.Count());
            
            return await Task.FromResult(connectedUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connected users");
            throw;
        }
    }

    /// <summary>
    /// Get connection count
    /// </summary>
    public async Task<int> GetConnectionCountAsync()
    {
        try
        {
            var count = _userConnections.Count;
            
            _logger.LogDebug("Current connection count: {Count}", count);
            
            return await Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connection count");
            throw;
        }
    }

    #region Static Helper Methods for Hub Access

    /// <summary>
    /// Get all connected user IDs (for hub access)
    /// </summary>
    public static IEnumerable<string> GetConnectedUserIds()
    {
        return _userConnections.Values.Distinct();
    }

    /// <summary>
    /// Get connection count (for hub access)
    /// </summary>
    public static int GetConnectionCount()
    {
        return _userConnections.Count;
    }

    /// <summary>
    /// Get connections for a specific role (for hub access)
    /// </summary>
    public static IEnumerable<string> GetConnectionsForRole(int role)
    {
        var roleGroupName = $"Role_{role}";
        return _roleGroups.TryGetValue(roleGroupName, out var connections) 
            ? connections.ToList() 
            : Enumerable.Empty<string>();
    }

    #endregion
}