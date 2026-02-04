using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers;

/// <summary>
/// Controller for WebSocket-related operations and information
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WebSocketController : ControllerBase
{
    private readonly ILogger<WebSocketController> _logger;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public WebSocketController(
        ILogger<WebSocketController> logger,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _logger = logger;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    /// <summary>
    /// Get WebSocket connection information
    /// </summary>
    [HttpGet("info")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWebSocketInfo()
    {
        try
        {
            var signalRSettings = _configuration.GetSection("SignalR");
            var hubPath = signalRSettings.GetValue<string>("HubPath", "/hubs/notifications");
            var useRedisBackplane = signalRSettings.GetValue<bool>("UseRedisBackplane", true);

            var connectionCount = await _notificationService.GetConnectionCountAsync();
            var connectedUsers = await _notificationService.GetConnectedUsersAsync();

            var info = new
            {
                HubEndpoint = $"{Request.Scheme}://{Request.Host}{hubPath}",
                ConnectionCount = connectionCount,
                ConnectedUserCount = connectedUsers.Count(),
                UseRedisBackplane = useRedisBackplane,
                SupportedProtocols = new[] { "json" },
                AuthenticationMethods = new[] { "JWT Bearer Token", "Query String Token" },
                Instructions = new
                {
                    Connection = $"Connect to: {Request.Scheme}://{Request.Host}{hubPath}",
                    Authentication = new
                    {
                        Method1 = "Add 'Authorization: Bearer <token>' header",
                        Method2 = "Add '?access_token=<token>' to connection URL"
                    },
                    Events = new
                    {
                        Notification = "General notifications",
                        DataChange = "Entity data change notifications",
                        ExportProgress = "Export job progress updates",
                        GroupMessage = "Group-specific messages",
                        UserConnected = "User connection events (admin only)",
                        UserDisconnected = "User disconnection events (admin only)"
                    },
                    Methods = new
                    {
                        JoinGroup = "Join a specific group",
                        LeaveGroup = "Leave a specific group",
                        SendToGroup = "Send message to group (admin only)",
                        GetConnectionStats = "Get connection statistics (admin only)",
                        Ping = "Health check ping"
                    }
                }
            };

            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WebSocket info");
            return StatusCode(500, new { Error = "Failed to retrieve WebSocket information" });
        }
    }

    /// <summary>
    /// Get connection statistics (admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetConnectionStats()
    {
        try
        {
            var connectionCount = await _notificationService.GetConnectionCountAsync();
            var connectedUsers = await _notificationService.GetConnectedUsersAsync();

            var stats = new
            {
                TotalConnections = connectionCount,
                UniqueUsers = connectedUsers.Count(),
                ConnectedUserIds = connectedUsers.ToArray(),
                Timestamp = DateTime.UtcNow,
                ServerInfo = new
                {
                    MachineName = Environment.MachineName,
                    ProcessId = Environment.ProcessId,
                    WorkingSet = Environment.WorkingSet,
                    TickCount = Environment.TickCount64
                }
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connection stats");
            return StatusCode(500, new { Error = "Failed to retrieve connection statistics" });
        }
    }

    /// <summary>
    /// Send a test notification to all connected clients (admin only)
    /// </summary>
    [HttpPost("test-notification")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SendTestNotification([FromBody] TestNotificationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { Error = "Message is required" });
            }

            var testData = new
            {
                TestId = Guid.NewGuid().ToString(),
                SentBy = User.Identity?.Name ?? "Unknown",
                SentAt = DateTime.UtcNow,
                CustomData = request.Data
            };

            await _notificationService.SendToAllAsync(request.Message, testData);

            _logger.LogInformation("Test notification sent by {User}: {Message}", 
                User.Identity?.Name, request.Message);

            return Ok(new
            {
                Success = true,
                Message = "Test notification sent successfully",
                TestData = testData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test notification");
            return StatusCode(500, new { Error = "Failed to send test notification" });
        }
    }

    /// <summary>
    /// Send a notification to a specific user (admin only)
    /// </summary>
    [HttpPost("notify-user/{userId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> NotifyUser(long userId, [FromBody] TestNotificationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { Error = "Message is required" });
            }

            var notificationData = new
            {
                NotificationId = Guid.NewGuid().ToString(),
                SentBy = User.Identity?.Name ?? "Unknown",
                SentAt = DateTime.UtcNow,
                TargetUserId = userId,
                CustomData = request.Data
            };

            await _notificationService.SendToUserAsync(userId, request.Message, notificationData);

            _logger.LogInformation("User notification sent by {User} to user {TargetUserId}: {Message}", 
                User.Identity?.Name, userId, request.Message);

            return Ok(new
            {
                Success = true,
                Message = $"Notification sent to user {userId}",
                NotificationData = notificationData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send user notification to {UserId}", userId);
            return StatusCode(500, new { Error = "Failed to send user notification" });
        }
    }

    /// <summary>
    /// Send a notification to users with a specific role (admin only)
    /// </summary>
    [HttpPost("notify-role/{role}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> NotifyRole(int role, [FromBody] TestNotificationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { Error = "Message is required" });
            }

            if (role < 0 || role > 2)
            {
                return BadRequest(new { Error = "Role must be 0 (User), 1 (Admin), or 2 (Manager)" });
            }

            var notificationData = new
            {
                NotificationId = Guid.NewGuid().ToString(),
                SentBy = User.Identity?.Name ?? "Unknown",
                SentAt = DateTime.UtcNow,
                TargetRole = role,
                RoleName = role switch { 0 => "User", 1 => "Admin", 2 => "Manager", _ => "Unknown" },
                CustomData = request.Data
            };

            await _notificationService.SendToRoleAsync(role, request.Message, notificationData);

            _logger.LogInformation("Role notification sent by {User} to role {TargetRole}: {Message}", 
                User.Identity?.Name, role, request.Message);

            return Ok(new
            {
                Success = true,
                Message = $"Notification sent to role {notificationData.RoleName}",
                NotificationData = notificationData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send role notification to role {Role}", role);
            return StatusCode(500, new { Error = "Failed to send role notification" });
        }
    }

    /// <summary>
    /// Test WebSocket connection health
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWebSocketHealth()
    {
        try
        {
            var connectionCount = await _notificationService.GetConnectionCountAsync();
            var signalRSettings = _configuration.GetSection("SignalR");
            var useRedisBackplane = signalRSettings.GetValue<bool>("UseRedisBackplane", true);

            var health = new
            {
                Status = "Healthy",
                ConnectionCount = connectionCount,
                UseRedisBackplane = useRedisBackplane,
                Timestamp = DateTime.UtcNow,
                Uptime = Environment.TickCount64
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket health check failed");
            return StatusCode(500, new 
            { 
                Status = "Unhealthy", 
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Request model for test notifications
/// </summary>
public class TestNotificationRequest
{
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}