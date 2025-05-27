using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleService> _logger;

        public RoleService(AppDbContext context, ILogger<RoleService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Roles>> GetAllRolesAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all roles with their permissions");
                var roles = await _context.Roles
                    .Include(r => r.RolePermissions!)
                    .ThenInclude(rp => rp.Permission!)
                    .ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} roles", roles.Count);
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all roles");
                throw new Exception("Failed to retrieve roles", ex);
            }
        }

        public async Task<Roles?> GetRoleByIdAsync(Guid roleId)
        {
            try
            {
                _logger.LogInformation("Retrieving role with ID: {RoleId}", roleId);
                var role = await _context.Roles
                    .Include(r => r.RolePermissions!)
                    .ThenInclude(rp => rp.Permission!)
                    .FirstOrDefaultAsync(r => r.RoleId == roleId);

                if (role == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found", roleId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved role: {RoleName}", role.Name);
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving role with ID: {RoleId}", roleId);
                throw new Exception($"Failed to retrieve role with ID: {roleId}", ex);
            }
        }

        public async Task<Roles?> GetRoleByLegacyIdAsync(int legacyRoleId)
        {
            try
            {
                _logger.LogInformation("Retrieving role with legacy ID: {LegacyRoleId}", legacyRoleId);
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.LegacyRoleId == legacyRoleId);

                if (role == null)
                {
                    _logger.LogWarning("Role with legacy ID {LegacyRoleId} not found", legacyRoleId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved role: {RoleName}", role.Name);
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving role with legacy ID: {LegacyRoleId}", legacyRoleId);
                throw new Exception($"Failed to retrieve role with legacy ID: {legacyRoleId}", ex);
            }
        }

        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId)
        {
            try
            {
                _logger.LogInformation("Retrieving permissions for role ID: {RoleId}", roleId);
                var role = await _context.Roles
                    .Include(r => r.RolePermissions!)
                    .ThenInclude(rp => rp.Permission!)
                    .FirstOrDefaultAsync(r => r.RoleId == roleId);

                if (role == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found while retrieving permissions", roleId);
                    return new List<Permission>();
                }

                var permissions = role.RolePermissions?
                    .Select(rp => rp.Permission!)
                    .Where(p => p != null)
                    .ToList() ?? new List<Permission>();

                _logger.LogInformation("Successfully retrieved {Count} permissions for role {RoleId}", 
                    permissions.Count, roleId);
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving permissions for role ID: {RoleId}", roleId);
                throw new Exception($"Failed to retrieve permissions for role ID: {roleId}", ex);
            }
        }

        public async Task<bool> AssignRoleToUserAsync(long userId, Guid roleId)
        {
            try
            {
                _logger.LogInformation("Assigning role {RoleId} to user {UserId}", roleId, userId);
                var user = await _context.Users.FindAsync(userId);
                var role = await _context.Roles.FindAsync(roleId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found while assigning role", userId);
                    return false;
                }

                if (role == null)
                {
                    _logger.LogWarning("Role {RoleId} not found while assigning to user", roleId);
                    return false;
                }

                var userRole = new UserRole
                {
                    UserId = user.GuidId,
                    RoleId = roleId,
                    AssignedAt = DateTime.UtcNow
                };

                await _context.UserRoles.AddAsync(userRole);
                user.Role = role.LegacyRoleId; // Keep legacy role in sync
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully assigned role {RoleName} to user {UserId}", 
                    role.Name, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning role {RoleId} to user {UserId}", 
                    roleId, userId);
                throw new Exception($"Failed to assign role {roleId} to user {userId}", ex);
            }
        }

        public async Task<bool> RemoveRoleFromUserAsync(long userId, Guid roleId)
        {
            try
            {
                _logger.LogInformation("Removing role {RoleId} from user {UserId}", roleId, userId);
                var userGuid = ConvertToGuid(userId);
                var userRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userGuid && ur.RoleId == roleId);

                if (userRole == null)
                {
                    _logger.LogWarning("User role association not found for user {UserId} and role {RoleId}", 
                        userId, roleId);
                    return false;
                }

                _context.UserRoles.Remove(userRole);

                // Update legacy role to lowest remaining role or default to user (0)
                var user = await _context.Users
                    .Include(u => u.UserRoles!)
                    .ThenInclude(ur => ur.Role!)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user != null)
                {
                    var highestRemainingRole = user.UserRoles?
                        .Where(ur => ur.RoleId != roleId)
                        .OrderByDescending(ur => ur.Role!.Priority)
                        .FirstOrDefault();

                    user.Role = highestRemainingRole?.Role?.LegacyRoleId ?? 0;
                    _logger.LogInformation("Updated user {UserId} legacy role to {LegacyRoleId}", 
                        userId, user.Role);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully removed role {RoleId} from user {UserId}", roleId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing role {RoleId} from user {UserId}", 
                    roleId, userId);
                throw new Exception($"Failed to remove role {roleId} from user {userId}", ex);
            }
        }

        // Helper methods for type conversion
        public static Guid ConvertToGuid(long value)
        {
            try
            {
                byte[] bytes = new byte[16];
                BitConverter.GetBytes(value).CopyTo(bytes, 0);
                return new Guid(bytes);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert long value {value} to GUID", ex);
            }
        }

        private static long ConvertToLong(Guid guid)
        {
            try
            {
                byte[] bytes = guid.ToByteArray();
                return BitConverter.ToInt64(bytes, 0);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert GUID {guid} to long", ex);
            }
        }

        // Helper method to check if running under SQLite
        private bool IsSqlite()
        {
            return _context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        // Helper method to generate provider-specific SQL for GUID conversion
        private string GetGuidConversionSql(string columnName)
        {
            return IsSqlite()
                ? $"CAST(SUBSTR(HEX({columnName}), 1, 16) AS TEXT)"  // SQLite
                : $"CAST({columnName} AS UNIQUEIDENTIFIER)";          // SQL Server
        }
    }
}
