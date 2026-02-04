using System.Collections.Generic;
using System.Threading.Tasks;

namespace TicketSalesApp.Services.Interfaces;

/// <summary>
/// Interface for notification hub context operations
/// This interface abstracts SignalR operations for the service layer
/// </summary>
public interface INotificationHubContext
{
    /// <summary>
    /// Send message to all clients
    /// </summary>
    Task SendToAllAsync(string method, object? data);

    /// <summary>
    /// Send message to specific group
    /// </summary>
    Task SendToGroupAsync(string groupName, string method, object? data);

    /// <summary>
    /// Add connection to group
    /// </summary>
    Task AddToGroupAsync(string connectionId, string groupName);

    /// <summary>
    /// Remove connection from group
    /// </summary>
    Task RemoveFromGroupAsync(string connectionId, string groupName);
}