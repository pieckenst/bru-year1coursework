using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.AdminServer.Authorization
{
    /// <summary>
    /// Authorization handler that accesses user roles from database context
    /// Fixes ASP.NET Core policy limitation by providing database-backed role checking
    /// </summary>
    public class DatabaseRoleAuthorizationHandler : AuthorizationHandler<DatabaseRoleRequirement>
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DatabaseRoleAuthorizationHandler> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

        public DatabaseRoleAuthorizationHandler(
            AppDbContext context,
            IMemoryCache cache,
            ILogger<DatabaseRoleAuthorizationHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DatabaseRoleRequirement requirement)
        {
            try
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("User ID claim not found or invalid in token");
                    context.Fail();
                    return;
                }

                // Check cache first
                var cacheKey = $"user_roles_{userId}";
                if (!_cache.TryGetValue(cacheKey, out UserRoleInfo? userRoleInfo))
                {
                    userRoleInfo = await GetUserRoleInfoFromDatabase(userId);
                    if (userRoleInfo != null)
                    {
                        _cache.Set(cacheKey, userRoleInfo, _cacheExpiration);
                    }
                }

                if (userRoleInfo == null)
                {
                    _logger.LogWarning("User {UserId} not found in database", userId);
                    context.Fail();
                    return;
                }

                // Check if user meets the requirement
                var hasRequiredRole = await CheckUserMeetsRequirement(userRoleInfo, requirement);
                if (hasRequiredRole)
                {
                    _logger.LogDebug("User {UserId} authorized for requirement {RequirementType}", 
                        userId, requirement.GetType().Name);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("User {UserId} does not meet authorization requirement {RequirementType}", 
                        userId, requirement.GetType().Name);
                    context.Fail();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authorization check for requirement {RequirementType}", 
                    requirement.GetType().Name);
                context.Fail();
            }
        }

        private async Task<UserRoleInfo?> GetUserRoleInfoFromDatabase(long userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles!)
                        .ThenInclude(ur => ur.Role!)
                            .ThenInclude(r => r.RolePermissions!)
                                .ThenInclude(rp => rp.Permission!)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return null;
                }

                var modernRoles = user.UserRoles?
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!)
                    .ToList() ?? new List<Roles>();

                var permissions = modernRoles
                    .SelectMany(r => r.RolePermissions ?? new List<RolePermission>())
                    .Where(rp => rp.Permission != null)
                    .Select(rp => rp.Permission!)
                    .Distinct()
                    .ToList();

                return new UserRoleInfo
                {
                    UserId = userId,
                    LegacyRole = user.Role,
                    ModernRoles = modernRoles,
                    Permissions = permissions,
                    IsActive = user.IsActive,
                    CachedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user role info for user {UserId}", userId);
                return null;
            }
        }

        private async Task<bool> CheckUserMeetsRequirement(UserRoleInfo userRoleInfo, DatabaseRoleRequirement requirement)
        {
            // Check if user is active
            if (!userRoleInfo.IsActive)
            {
                return false;
            }

            return requirement switch
            {
                AdminOnlyRequirement => CheckAdminAccess(userRoleInfo),
                PermissionRequirement permReq => CheckPermissionAccess(userRoleInfo, permReq.RequiredPermission),
                RoleRequirement roleReq => CheckRoleAccess(userRoleInfo, roleReq.RequiredRoles),
                LegacyRoleRequirement legacyReq => CheckLegacyRoleAccess(userRoleInfo, legacyReq.MinimumLegacyRole),
                _ => false
            };
        }

        private bool CheckAdminAccess(UserRoleInfo userRoleInfo)
        {
            // Check legacy role first (backward compatibility)
            if (userRoleInfo.LegacyRole >= 1)
            {
                return true;
            }

            // Check modern RBAC system
            return userRoleInfo.ModernRoles.Any(r => 
                r.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                r.LegacyRoleId >= 1);
        }

        private bool CheckPermissionAccess(UserRoleInfo userRoleInfo, string requiredPermission)
        {
            // Admin always has access (legacy compatibility)
            if (CheckAdminAccess(userRoleInfo))
            {
                return true;
            }

            // Check specific permission
            return userRoleInfo.Permissions.Any(p => 
                p.Name.Equals(requiredPermission, StringComparison.OrdinalIgnoreCase));
        }

        private bool CheckRoleAccess(UserRoleInfo userRoleInfo, IEnumerable<string> requiredRoles)
        {
            // Admin always has access (legacy compatibility)
            if (CheckAdminAccess(userRoleInfo))
            {
                return true;
            }

            // Check if user has any of the required roles
            return userRoleInfo.ModernRoles.Any(userRole =>
                requiredRoles.Any(requiredRole =>
                    userRole.Name.Equals(requiredRole, StringComparison.OrdinalIgnoreCase)));
        }

        private bool CheckLegacyRoleAccess(UserRoleInfo userRoleInfo, int minimumLegacyRole)
        {
            return userRoleInfo.LegacyRole >= minimumLegacyRole;
        }
    }

    /// <summary>
    /// Cached user role information
    /// </summary>
    public class UserRoleInfo
    {
        public long UserId { get; set; }
        public int LegacyRole { get; set; }
        public List<Roles> ModernRoles { get; set; } = new();
        public List<Permission> Permissions { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CachedAt { get; set; }
    }
}