using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;
using Serilog;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRoleService _roleService;

        public RolesController(AppDbContext context, IRoleService roleService)
        {
            _context = context;
            _roleService = roleService;
        }

        private bool IsAdmin()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return false;

            var token = authHeader.Substring("Bearer ".Length);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
            return roleClaim?.Value == "1";
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Roles>>> GetRoles()
        {
            Log.Information("Fetching all roles");
            return await _context.Roles
                .Include(r => r.RolePermissions!)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Roles>> GetRole(Guid id)
        {
            Log.Information("Fetching role with ID {RoleId}", id);
            var role = await _context.Roles
                .Include(r => r.RolePermissions!)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                Log.Warning("Role with ID {RoleId} not found", id);
                return NotFound();
            }

            return role;
        }

        [HttpGet("{id}/permissions")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetRolePermissions(Guid id)
        {
            Log.Information("Fetching permissions for role {RoleId}", id);
            var permissions = await _roleService.GetRolePermissionsAsync(id);
            return Ok(permissions);
        }

        [HttpPost]
        public async Task<ActionResult<Roles>> CreateRole([FromBody] CreateRoleModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to create role by non-admin user");
                return Forbid();
            }

            Log.Information("Creating new role: {RoleName}", model.Name);
            var role = new Roles
            {
                RoleId = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                LegacyRoleId = model.LegacyRoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Priority = model.Priority,
                IsSystem = false
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Add permissions if specified
            if (model.PermissionIds != null && model.PermissionIds.Any())
            {
                var rolePermissions = model.PermissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = role.RoleId,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = User.Identity?.Name ?? "System"
                });

                await _context.RolePermissions.AddRangeAsync(rolePermissions);
                await _context.SaveChangesAsync();
            }

            Log.Information("Successfully created role {RoleName} with ID {RoleId}", role.Name, role.RoleId);
            return CreatedAtAction(nameof(GetRole), new { id = role.RoleId }, role);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update role by non-admin user");
                return Forbid();
            }

            Log.Information("Updating role {RoleId}", id);
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                Log.Warning("Role with ID {RoleId} not found for update", id);
                return NotFound();
            }

            if (role.IsSystem)
            {
                Log.Warning("Attempted to modify system role {RoleId}", id);
                return BadRequest("System roles cannot be modified");
            }

            role.Name = model.Name ?? role.Name;
            role.Description = model.Description ?? role.Description;
            role.Priority = model.Priority ?? role.Priority;
            role.UpdatedAt = DateTime.UtcNow;

            if (model.PermissionIds != null)
            {
                // Remove existing permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();
                _context.RolePermissions.RemoveRange(existingPermissions);

                // Add new permissions
                var newPermissions = model.PermissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = User.Identity?.Name ?? "System"
                });
                await _context.RolePermissions.AddRangeAsync(newPermissions);
            }

            await _context.SaveChangesAsync();
            Log.Information("Successfully updated role {RoleId}", id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete role by non-admin user");
                return Forbid();
            }

            Log.Information("Attempting to delete role {RoleId}", id);
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                Log.Warning("Role with ID {RoleId} not found for deletion", id);
                return NotFound();
            }

            if (role.IsSystem)
            {
                Log.Warning("Attempted to delete system role {RoleId}", id);
                return BadRequest("System roles cannot be deleted");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            Log.Information("Successfully deleted role {RoleId}", id);
            return NoContent();
        }
    }

    public class CreateRoleModel
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int LegacyRoleId { get; set; }
        public int Priority { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }

    public class UpdateRoleModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Priority { get; set; }
        public List<Guid>? PermissionIds { get; set; }
    }
} 