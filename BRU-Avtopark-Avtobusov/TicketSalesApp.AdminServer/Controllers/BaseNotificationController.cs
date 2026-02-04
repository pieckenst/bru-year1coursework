using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers;

/// <summary>
/// Base controller that provides notification capabilities to derived controllers
/// </summary>
public abstract class BaseNotificationController : ControllerBase
{
    protected readonly INotificationService NotificationService;
    protected readonly ILogger Logger;

    protected BaseNotificationController(INotificationService notificationService, ILogger logger)
    {
        NotificationService = notificationService;
        Logger = logger;
    }

    /// <summary>
    /// Get the current user's ID from claims
    /// </summary>
    protected long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Get the current user's role from claims
    /// </summary>
    protected int GetCurrentUserRole()
    {
        var roleClaim = User.FindFirst("role")?.Value;
        return int.TryParse(roleClaim, out var role) ? role : 0; // Default to User role
    }

    /// <summary>
    /// Broadcast data change notification for entity operations
    /// </summary>
    protected async Task NotifyDataChangeAsync(string entityType, string action, long entityId, object? data = null)
    {
        try
        {
            await NotificationService.BroadcastDataChangeAsync(entityType, action, entityId, data);
            Logger.LogDebug("Data change notification sent: {EntityType} {Action} (ID: {EntityId})", 
                entityType, action, entityId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send data change notification: {EntityType} {Action} (ID: {EntityId})", 
                entityType, action, entityId);
            // Don't throw - notification failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Send notification to admins only
    /// </summary>
    protected async Task NotifyAdminsAsync(string message, object? data = null)
    {
        try
        {
            await NotificationService.SendToRoleAsync(1, message, data); // Role 1 = Admin
            Logger.LogDebug("Admin notification sent: {Message}", message);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send admin notification: {Message}", message);
            // Don't throw - notification failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Send notification to current user
    /// </summary>
    protected async Task NotifyCurrentUserAsync(string message, object? data = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await NotificationService.SendToUserAsync(userId.Value, message, data);
                Logger.LogDebug("User notification sent to {UserId}: {Message}", userId.Value, message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send user notification: {Message}", message);
            // Don't throw - notification failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Send notification to all authenticated users
    /// </summary>
    protected async Task NotifyAllUsersAsync(string message, object? data = null)
    {
        try
        {
            await NotificationService.SendToAllAsync(message, data);
            Logger.LogDebug("Broadcast notification sent: {Message}", message);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send broadcast notification: {Message}", message);
            // Don't throw - notification failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Helper method to create standardized notification data
    /// </summary>
    protected object CreateNotificationData(string action, object? entityData = null, string? additionalInfo = null)
    {
        return new
        {
            Action = action,
            UserId = GetCurrentUserId(),
            UserRole = GetCurrentUserRole(),
            Timestamp = DateTime.UtcNow,
            EntityData = entityData,
            AdditionalInfo = additionalInfo,
            RequestId = HttpContext.TraceIdentifier
        };
    }
}