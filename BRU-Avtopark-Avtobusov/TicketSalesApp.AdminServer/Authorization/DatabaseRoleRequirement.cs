using Microsoft.AspNetCore.Authorization;

namespace TicketSalesApp.AdminServer.Authorization
{
    /// <summary>
    /// Base class for database-backed authorization requirements
    /// </summary>
    public abstract class DatabaseRoleRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Requirement for admin-only access (legacy Role = 1 or Administrator role)
    /// </summary>
    public class AdminOnlyRequirement : DatabaseRoleRequirement
    {
    }

    /// <summary>
    /// Requirement for specific permission access
    /// </summary>
    public class PermissionRequirement : DatabaseRoleRequirement
    {
        public string RequiredPermission { get; }

        public PermissionRequirement(string requiredPermission)
        {
            RequiredPermission = requiredPermission ?? throw new ArgumentNullException(nameof(requiredPermission));
        }
    }

    /// <summary>
    /// Requirement for specific role access (modern RBAC)
    /// </summary>
    public class RoleRequirement : DatabaseRoleRequirement
    {
        public IEnumerable<string> RequiredRoles { get; }

        public RoleRequirement(params string[] requiredRoles)
        {
            RequiredRoles = requiredRoles ?? throw new ArgumentNullException(nameof(requiredRoles));
        }

        public RoleRequirement(IEnumerable<string> requiredRoles)
        {
            RequiredRoles = requiredRoles ?? throw new ArgumentNullException(nameof(requiredRoles));
        }
    }

    /// <summary>
    /// Requirement for legacy role access (backward compatibility)
    /// </summary>
    public class LegacyRoleRequirement : DatabaseRoleRequirement
    {
        public int MinimumLegacyRole { get; }

        public LegacyRoleRequirement(int minimumLegacyRole)
        {
            MinimumLegacyRole = minimumLegacyRole;
        }
    }
}