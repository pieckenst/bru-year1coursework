using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using TicketSalesApp.AdminServer.Hubs;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Services;

/// <summary>
/// SignalR implementation of INotificationHubContext
/// </summary>
public class SignalRNotificationHubContext : INotificationHubContext
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationHubContext(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    public async Task SendToAllAsync(string method, object? data)
    {
        await _hubContext.Clients.All.SendAsync(method, data);
    }

    /// <summary>
    /// Send message to specific group
    /// </summary>
    public async Task SendToGroupAsync(string groupName, string method, object? data)
    {
        await _hubContext.Clients.Group(groupName).SendAsync(method, data);
    }

    /// <summary>
    /// Add connection to group
    /// </summary>
    public async Task AddToGroupAsync(string connectionId, string groupName)
    {
        await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
    }

    /// <summary>
    /// Remove connection from group
    /// </summary>
    public async Task RemoveFromGroupAsync(string connectionId, string groupName)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
    }
}