using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Service for notifying when user roles change to invalidate caches
    /// </summary>
    public class UserRoleChangeNotificationService : IUserRoleChangeNotificationService
    {
        private readonly IRoleCacheService _roleCacheService;
        private readonly AppDbContext _context;
        private readonly ILogger<UserRoleChangeNotificationService> _logger;

        public UserRoleChangeNotificationService(
            IRoleCacheService roleCacheService,
            AppDbContext context,
            ILogger<UserRoleChangeNotificationService> logger)
        {
            _roleCacheService = roleCacheService ?? throw new ArgumentNullException(nameof(roleCacheService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task NotifyUserRoleChangedAsync(long userId)
        {
            try
            {
                await _roleCacheService.InvalidateUserRolesAsync(userId);
                _logger.LogInformation("Invalidated role cache for user {UserId} due to role change", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating role cache for user {UserId}", userId);
            }
        }

        public async Task NotifyUserRolesChangedAsync(IEnumerable<long> userIds)
        {
            try
            {
                await _roleCacheService.InvalidateUserRolesAsync(userIds);
                var userIdList = userIds.ToList();
                _logger.LogInformation("Invalidated role cache for {Count} users due to role changes", userIdList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating role cache for multiple users");
            }
        }

        public async Task NotifyRolePermissionsChangedAsync(Guid roleId)
        {
            try
            {
                // Find all users with this role and invalidate their cache
                var userIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == roleId)
                    .Join(_context.Users, ur => ur.UserId, u => u.GuidId, (ur, u) => u.UserId)
                    .ToListAsync();

                if (userIds.Any())
                {
                    await _roleCacheService.InvalidateUserRolesAsync(userIds);
                    _logger.LogInformation("Invalidated role cache for {Count} users due to role {RoleId} permission changes", 
                        userIds.Count, roleId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating role cache for role {RoleId} permission changes", roleId);
            }
        }

        public async Task NotifyAllRoleDataChangedAsync()
        {
            try
            {
                await _roleCacheService.InvalidateAllRoleCacheAsync();
                _logger.LogInformation("Invalidated all role cache data due to system-wide role changes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating all role cache data");
            }
        }
    }
}