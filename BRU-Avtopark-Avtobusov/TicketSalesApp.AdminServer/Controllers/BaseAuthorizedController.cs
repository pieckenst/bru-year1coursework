using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Base controller with authorization helper methods
    /// Replaces manual IsAdmin() checks with policy-based authorization
    /// </summary>
    [ApiController]
    [Authorize] // All controllers require authentication by default
    public abstract class BaseAuthorizedController : ControllerBase
    {
        protected readonly ILogger _logger;
        protected readonly IRoleCacheService _roleCacheService;

        protected BaseAuthorizedController(ILogger logger, IRoleCacheService roleCacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roleCacheService = roleCacheService ?? throw new ArgumentNullException(nameof(roleCacheService));
        }

        /// <summary>
        /// Get the current user's ID from JWT token
        /// </summary>
        protected long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Get the current user's legacy role from JWT token
        /// </summary>
        protected int? GetCurrentUserLegacyRole()
        {
            var roleClaim = User.FindFirst("role");
            if (roleClaim != null && int.TryParse(roleClaim.Value, out var role))
            {
                return role;
            }
            return null;
        }

        /// <summary>
        /// Check if current user is admin (legacy compatibility)
        /// This method is deprecated - use [Authorize(Policy = "AdminOnly")] instead
        /// </summary>
        [Obsolete("Use [Authorize(Policy = \"AdminOnly\")] attribute instead of manual checks")]
        protected bool IsAdmin()
        {
            var role = GetCurrentUserLegacyRole();
            return role.HasValue && role.Value >= 1;
        }

        /// <summary>
        /// Log unauthorized access attempt
        /// </summary>
        protected void LogUnauthorizedAttempt(string action, object? additionalData = null)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserLegacyRole();
            
            _logger.LogWarning("Unauthorized attempt to {Action} by user {UserId} with role {Role}. Additional data: {@AdditionalData}",
                action, userId, role, additionalData);
        }

        /// <summary>
        /// Log successful authorized action
        /// </summary>
        protected void LogAuthorizedAction(string action, object? additionalData = null)
        {
            var userId = GetCurrentUserId();
            
            _logger.LogInformation("User {UserId} successfully performed {Action}. Additional data: {@AdditionalData}",
                userId, action, additionalData);
        }

        /// <summary>
        /// Invalidate role cache for current user (call when user roles change)
        /// </summary>
        protected async Task InvalidateCurrentUserRoleCache()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _roleCacheService.InvalidateUserRolesAsync(userId.Value);
            }
        }

        /// <summary>
        /// Preload role cache for current user (call for performance optimization)
        /// </summary>
        protected async Task PreloadCurrentUserRoleCache()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _roleCacheService.PreloadUserRoleInfoAsync(userId.Value);
            }
        }

        /// <summary>
        /// Create a standardized unauthorized response
        /// </summary>
        protected IActionResult CreateUnauthorizedResponse(string action)
        {
            LogUnauthorizedAttempt(action);
            return Forbid($"Insufficient permissions to {action}");
        }

        /// <summary>
        /// Create a standardized not found response
        /// </summary>
        protected IActionResult CreateNotFoundResponse(string resourceType, object id)
        {
            _logger.LogWarning("{ResourceType} with ID {Id} not found", resourceType, id);
            return NotFound(new { Message = $"{resourceType} not found", Id = id });
        }

        /// <summary>
        /// Create a standardized validation error response
        /// </summary>
        protected IActionResult CreateValidationErrorResponse(string message, object? errors = null)
        {
            _logger.LogWarning("Validation error: {Message}. Errors: {@Errors}", message, errors);
            return BadRequest(new { Message = message, Errors = errors });
        }

        /// <summary>
        /// Create a standardized success response
        /// </summary>
        protected IActionResult CreateSuccessResponse(string action, object? data = null)
        {
            LogAuthorizedAction(action, data);
            return Ok(new { Message = $"Successfully {action}", Data = data });
        }
    }
}