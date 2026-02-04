using System.Threading.Tasks;
using System.Collections.Generic;

namespace TicketSalesApp.Services.Interfaces;

/// <summary>
/// Service for managing real-time notifications via WebSocket connections
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification to all connected clients
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="data">Optional data payload</param>
    Task SendToAllAsync(string message, object? data = null);

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="message">Notification message</param>
    /// <param name="data">Optional data payload</param>
    Task SendToUserAsync(long userId, string message, object? data = null);

    /// <summary>
    /// Send notification to users with specific role
    /// </summary>
    /// <param name="role">Target role (0=User, 1=Admin, 2=Manager)</param>
    /// <param name="message">Notification message</param>
    /// <param name="data">Optional data payload</param>
    Task SendToRoleAsync(int role, string message, object? data = null);

    /// <summary>
    /// Send notification to specific group
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="message">Notification message</param>
    /// <param name="data">Optional data payload</param>
    Task SendToGroupAsync(string groupName, string message, object? data = null);

    /// <summary>
    /// Add user to a group
    /// </summary>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <param name="groupName">Group name</param>
    Task AddToGroupAsync(string connectionId, string groupName);

    /// <summary>
    /// Remove user from a group
    /// </summary>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <param name="groupName">Group name</param>
    Task RemoveFromGroupAsync(string connectionId, string groupName);

    /// <summary>
    /// Broadcast data change notification
    /// </summary>
    /// <param name="entityType">Type of entity that changed (Bus, Route, Ticket, User, etc.)</param>
    /// <param name="action">Action performed (Created, Updated, Deleted)</param>
    /// <param name="entityId">ID of the changed entity</param>
    /// <param name="data">Changed entity data</param>
    Task BroadcastDataChangeAsync(string entityType, string action, long entityId, object? data = null);

    /// <summary>
    /// Send export progress notification
    /// </summary>
    /// <param name="userId">User who initiated the export</param>
    /// <param name="exportId">Export job ID</param>
    /// <param name="progress">Progress percentage (0-100)</param>
    /// <param name="status">Export status</param>
    /// <param name="message">Status message</param>
    Task SendExportProgressAsync(long userId, string exportId, int progress, string status, string? message = null);

    /// <summary>
    /// Get list of connected users
    /// </summary>
    Task<IEnumerable<string>> GetConnectedUsersAsync();

    /// <summary>
    /// Get connection count
    /// </summary>
    Task<int> GetConnectionCountAsync();
}