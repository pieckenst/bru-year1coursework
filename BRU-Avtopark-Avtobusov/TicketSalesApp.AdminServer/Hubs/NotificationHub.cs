using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using TicketSalesApp.Services.Interfaces;
using TicketSalesApp.Services.Implementations;

namespace TicketSalesApp.AdminServer.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications with authentication and group management
/// </summary>
[Authorize] // Require authentication for all hub methods
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly IAuthenticationService _authService;

    public NotificationHub(ILogger<NotificationHub> logger, IAuthenticationService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var connectionId = Context.ConnectionId;

            if (userId != null)
            {
                // Register connection using NotificationService
                NotificationService.RegisterConnection(connectionId, userId, userRole);

                // Add to role-based group
                var roleGroupName = $"Role_{userRole}";
                await Groups.AddToGroupAsync(connectionId, roleGroupName);

                // Add to user-specific group
                var userGroupName = $"User_{userId}";
                await Groups.AddToGroupAsync(connectionId, userGroupName);

                _logger.LogInformation("User {UserId} connected with connection {ConnectionId} in role {Role}", 
                    userId, connectionId, userRole);

                // Notify other admins about new connection (if user is admin)
                if (userRole == 1) // Admin role
                {
                    await Clients.Group("Role_1").SendAsync("UserConnected", new
                    {
                        UserId = userId,
                        ConnectionId = connectionId,
                        Timestamp = DateTime.UtcNow,
                        Role = userRole
                    });
                }
            }
            else
            {
                _logger.LogWarning("Anonymous connection attempt from {ConnectionId}", connectionId);
                Context.Abort(); // Disconnect anonymous users
                return;
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection for {ConnectionId}", Context.ConnectionId);
            throw;
        }
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var userId = GetUserId();
            var userRole = GetUserRole();

            if (userId != null)
            {
                // Unregister connection using NotificationService
                NotificationService.UnregisterConnection(connectionId, userRole);

                _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}. Reason: {Exception}", 
                    userId, connectionId, exception?.Message ?? "Normal disconnect");

                // Notify other admins about disconnection (if user is admin)
                if (userRole == 1) // Admin role
                {
                    await Clients.Group("Role_1").SendAsync("UserDisconnected", new
                    {
                        UserId = userId,
                        ConnectionId = connectionId,
                        Timestamp = DateTime.UtcNow,
                        Reason = exception?.Message ?? "Normal disconnect"
                    });
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Join a specific group (requires appropriate permissions)
    /// </summary>
    /// <param name="groupName">Name of the group to join</param>
    [HubMethodName("JoinGroup")]
    public async Task JoinGroupAsync(string groupName)
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var connectionId = Context.ConnectionId;

            // Validate group access based on user role
            if (!CanAccessGroup(groupName, userRole))
            {
                _logger.LogWarning("User {UserId} attempted to join unauthorized group {GroupName}", userId, groupName);
                await Clients.Caller.SendAsync("Error", "Access denied to group: " + groupName);
                return;
            }

            await Groups.AddToGroupAsync(connectionId, groupName);
            
            _logger.LogInformation("User {UserId} joined group {GroupName}", userId, groupName);
            
            await Clients.Caller.SendAsync("GroupJoined", new
            {
                GroupName = groupName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining group {GroupName} for user {UserId}", groupName, GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to join group: " + ex.Message);
        }
    }

    /// <summary>
    /// Leave a specific group
    /// </summary>
    /// <param name="groupName">Name of the group to leave</param>
    [HubMethodName("LeaveGroup")]
    public async Task LeaveGroupAsync(string groupName)
    {
        try
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            await Groups.RemoveFromGroupAsync(connectionId, groupName);
            
            _logger.LogInformation("User {UserId} left group {GroupName}", userId, groupName);
            
            await Clients.Caller.SendAsync("GroupLeft", new
            {
                GroupName = groupName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving group {GroupName} for user {UserId}", groupName, GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to leave group: " + ex.Message);
        }
    }

    /// <summary>
    /// Send a message to a specific group (admin only)
    /// </summary>
    /// <param name="groupName">Target group name</param>
    /// <param name="message">Message to send</param>
    [HubMethodName("SendToGroup")]
    [Authorize(Policy = "AdminOnly")]
    public async Task SendToGroupAsync(string groupName, string message)
    {
        try
        {
            var userId = GetUserId();
            
            await Clients.Group(groupName).SendAsync("GroupMessage", new
            {
                GroupName = groupName,
                Message = message,
                SenderId = userId,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Admin {UserId} sent message to group {GroupName}: {Message}", userId, groupName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to group {GroupName} from user {UserId}", groupName, GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to send message: " + ex.Message);
        }
    }

    /// <summary>
    /// Get connection statistics (admin only)
    /// </summary>
    [HubMethodName("GetConnectionStats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task GetConnectionStatsAsync()
    {
        try
        {
            var connectionCount = NotificationService.GetConnectionCount();
            var connectedUsers = NotificationService.GetConnectedUserIds();

            var stats = new
            {
                TotalConnections = connectionCount,
                UniqueUsers = connectedUsers.Count(),
                ConnectedUserIds = connectedUsers.ToArray(),
                Timestamp = DateTime.UtcNow
            };

            await Clients.Caller.SendAsync("ConnectionStats", stats);
            
            _logger.LogInformation("Connection stats requested by admin {UserId}: {Stats}", GetUserId(), stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection stats for user {UserId}", GetUserId());
            await Clients.Caller.SendAsync("Error", "Failed to get connection stats: " + ex.Message);
        }
    }

    /// <summary>
    /// Ping method for connection health check
    /// </summary>
    [HubMethodName("Ping")]
    public async Task PingAsync()
    {
        await Clients.Caller.SendAsync("Pong", new
        {
            Timestamp = DateTime.UtcNow,
            ConnectionId = Context.ConnectionId
        });
    }

    #region Private Helper Methods

    /// <summary>
    /// Get the current user's ID from claims
    /// </summary>
    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Get the current user's role from claims
    /// </summary>
    private int GetUserRole()
    {
        var roleClaim = Context.User?.FindFirst("role")?.Value;
        return int.TryParse(roleClaim, out var role) ? role : 0; // Default to User role
    }

    /// <summary>
    /// Check if user can access a specific group based on their role
    /// </summary>
    private bool CanAccessGroup(string groupName, int userRole)
    {
        // Define group access rules
        return groupName switch
        {
            // Admin-only groups
            var g when g.StartsWith("Admin_") => userRole == 1,
            var g when g.StartsWith("Role_1") => userRole == 1,
            
            // Manager and Admin groups
            var g when g.StartsWith("Manager_") => userRole >= 1, // Manager (2) or Admin (1)
            var g when g.StartsWith("Role_2") => userRole >= 1,
            
            // Public groups (all authenticated users)
            var g when g.StartsWith("Public_") => true,
            var g when g.StartsWith("Notifications_") => true,
            
            // User-specific groups (only the user themselves)
            var g when g.StartsWith("User_") => g == $"User_{GetUserId()}",
            
            // Default: deny access
            _ => false
        };
    }

    #endregion
}