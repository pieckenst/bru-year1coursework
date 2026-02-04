using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Demonstration controller showing the new policy-based authorization system
    /// This controller demonstrates how to replace manual IsAdmin() checks with declarative policies
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthorizationDemoController : BaseAuthorizedController
    {
        public AuthorizationDemoController(
            ILogger<AuthorizationDemoController> logger,
            IRoleCacheService roleCacheService)
            : base(logger, roleCacheService)
        {
        }

        /// <summary>
        /// Endpoint accessible to any authenticated user
        /// </summary>
        [HttpGet("public")]
        [Authorize] // Basic authentication required
        public IActionResult GetPublicData()
        {
            LogAuthorizedAction("access public data");
            return Ok(new
            {
                Message = "This endpoint is accessible to any authenticated user",
                UserId = GetCurrentUserId(),
                UserRole = GetCurrentUserLegacyRole(),
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Endpoint accessible only to administrators (legacy compatibility)
        /// </summary>
        [HttpGet("admin-only")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult GetAdminOnlyData()
        {
            LogAuthorizedAction("access admin-only data");
            return Ok(new
            {
                Message = "This endpoint is accessible only to administrators",
                UserId = GetCurrentUserId(),
                UserRole = GetCurrentUserLegacyRole(),
                Timestamp = DateTime.UtcNow,
                AdminFeatures = new[]
                {
                    "User Management",
                    "System Configuration",
                    "Advanced Reports",
                    "Role Management"
                }
            });
        }

        /// <summary>
        /// Endpoint for users with bus management permissions
        /// </summary>
        [HttpGet("bus-management")]
        [Authorize(Policy = AuthorizationPolicies.CanManageBuses)]
        public IActionResult GetBusManagementData()
        {
            LogAuthorizedAction("access bus management data");
            return Ok(new
            {
                Message = "This endpoint is accessible to users with bus management permissions",
                UserId = GetCurrentUserId(),
                Permissions = new[] { "Create Buses", "Edit Buses", "Delete Buses", "View Bus Reports" },
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Endpoint for users with route management permissions
        /// </summary>
        [HttpGet("route-management")]
        [Authorize(Policy = AuthorizationPolicies.CanManageRoutes)]
        public IActionResult GetRouteManagementData()
        {
            LogAuthorizedAction("access route management data");
            return Ok(new
            {
                Message = "This endpoint is accessible to users with route management permissions",
                UserId = GetCurrentUserId(),
                Permissions = new[] { "Create Routes", "Edit Routes", "Delete Routes", "View Route Analytics" },
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Endpoint for users with report viewing permissions
        /// </summary>
        [HttpGet("reports")]
        [Authorize(Policy = AuthorizationPolicies.CanViewReports)]
        public IActionResult GetReportsData()
        {
            LogAuthorizedAction("access reports data");
            return Ok(new
            {
                Message = "This endpoint is accessible to users with report viewing permissions",
                UserId = GetCurrentUserId(),
                AvailableReports = new[]
                {
                    "Sales Summary",
                    "Route Performance",
                    "Bus Utilization",
                    "Employee Performance"
                },
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Endpoint for users with data export permissions
        /// </summary>
        [HttpGet("export")]
        [Authorize(Policy = AuthorizationPolicies.CanExportData)]
        public IActionResult GetExportData()
        {
            LogAuthorizedAction("access export data");
            return Ok(new
            {
                Message = "This endpoint is accessible to users with data export permissions",
                UserId = GetCurrentUserId(),
                ExportFormats = new[] { "CSV", "Excel", "JSON", "PDF" },
                ExportTypes = new[] { "Buses", "Routes", "Tickets", "Sales", "Users" },
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Endpoint demonstrating role cache management
        /// </summary>
        [HttpPost("invalidate-cache")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> InvalidateRoleCache()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await InvalidateCurrentUserRoleCache();
                LogAuthorizedAction("invalidate role cache", new { UserId = userId });
                
                return Ok(new
                {
                    Message = "Role cache invalidated successfully",
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }

            return BadRequest("Unable to determine current user ID");
        }

        /// <summary>
        /// Endpoint demonstrating cache statistics (admin only)
        /// </summary>
        [HttpGet("cache-stats")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public async Task<IActionResult> GetCacheStatistics()
        {
            var stats = await _roleCacheService.GetCacheStatisticsAsync();
            LogAuthorizedAction("view cache statistics");
            
            return Ok(new
            {
                Message = "Role cache statistics",
                Statistics = stats,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Endpoint showing all available authorization policies
        /// </summary>
        [HttpGet("policies")]
        [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
        public IActionResult GetAuthorizationPolicies()
        {
            LogAuthorizedAction("view authorization policies");
            
            return Ok(new
            {
                Message = "Available authorization policies",
                Policies = AuthorizationPolicies.GetAllPolicyNames().ToArray(),
                Permissions = AuthorizationPolicies.GetAllPermissionNames().ToArray(),
                Timestamp = DateTime.UtcNow,
                Documentation = new
                {
                    AdminOnly = "Legacy admin role (Role = 1) or Administrator role in RBAC",
                    CanManageBuses = "Users with 'Create Buses' permission",
                    CanManageRoutes = "Users with 'Create Routes' permission",
                    CanViewReports = "Users with 'View Reports' permission",
                    CanExportData = "Users with 'View Reports' permission (can export data)",
                    CanViewDashboard = "Any authenticated user"
                }
            });
        }

        /// <summary>
        /// Endpoint demonstrating the deprecated IsAdmin() method (for comparison)
        /// </summary>
        [HttpGet("legacy-admin-check")]
        [Authorize]
        public IActionResult LegacyAdminCheck()
        {
            // This demonstrates the old way (deprecated)
            #pragma warning disable CS0618 // Type or member is obsolete
            var isAdminLegacy = IsAdmin();
            #pragma warning restore CS0618 // Type or member is obsolete
            
            LogAuthorizedAction("legacy admin check", new { IsAdminLegacy = isAdminLegacy });
            
            return Ok(new
            {
                Message = "Legacy admin check (deprecated - use [Authorize(Policy = \"AdminOnly\")] instead)",
                IsAdminLegacy = isAdminLegacy,
                UserId = GetCurrentUserId(),
                UserRole = GetCurrentUserLegacyRole(),
                Recommendation = "Replace manual IsAdmin() checks with [Authorize(Policy = \"AdminOnly\")] attribute",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}