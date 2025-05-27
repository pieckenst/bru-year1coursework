using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.Services.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<Roles>> GetAllRolesAsync();
        Task<Roles?> GetRoleByIdAsync(Guid roleId);
        Task<Roles?> GetRoleByLegacyIdAsync(int legacyRoleId);
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId);
        Task<bool> AssignRoleToUserAsync(long userId, Guid roleId);
        Task<bool> RemoveRoleFromUserAsync(long userId, Guid roleId);
    }
}
