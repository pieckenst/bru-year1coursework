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
using Serilog;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
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
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissions()
        {
            Log.Information("Fetching all permissions");
            return await _context.Permissions
                .Include(p => p.RolePermissions!)
                .ThenInclude(rp => rp.Role)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Permission>> GetPermission(Guid id)
        {
            Log.Information("Fetching permission with ID {PermissionId}", id);
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions!)
                .ThenInclude(rp => rp.Role)
                .FirstOrDefaultAsync(p => p.PermissionId == id);

            if (permission == null)
            {
                Log.Warning("Permission with ID {PermissionId} not found", id);
                return NotFound();
            }

            return permission;
        }

        [HttpPost]
        public async Task<ActionResult<Permission>> CreatePermission([FromBody] CreatePermissionModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to create permission by non-admin user");
                return Forbid();
            }

            Log.Information("Creating new permission: {PermissionName}", model.Name);

            // Check if permission with same name exists
            if (await _context.Permissions.AnyAsync(p => p.Name == model.Name))
            {
                Log.Warning("Permission with name {PermissionName} already exists", model.Name);
                return BadRequest("Permission with this name already exists");
            }

            var permission = new Permission
            {
                PermissionId = Guid.NewGuid(),
                Name = model.Name,
                Description = model.Description,
                Category = model.Category,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            Log.Information("Successfully created permission {PermissionName} with ID {PermissionId}", 
                permission.Name, permission.PermissionId);
            return CreatedAtAction(nameof(GetPermission), new { id = permission.PermissionId }, permission);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update permission by non-admin user");
                return Forbid();
            }

            Log.Information("Updating permission {PermissionId}", id);
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                Log.Warning("Permission with ID {PermissionId} not found for update", id);
                return NotFound();
            }

            // Check if new name conflicts with existing permission
            if (model.Name != null && model.Name != permission.Name)
            {
                if (await _context.Permissions.AnyAsync(p => p.Name == model.Name))
                {
                    Log.Warning("Permission with name {PermissionName} already exists", model.Name);
                    return BadRequest("Permission with this name already exists");
                }
                permission.Name = model.Name;
            }

            permission.Description = model.Description ?? permission.Description;
            permission.Category = model.Category ?? permission.Category;
            permission.IsActive = model.IsActive ?? permission.IsActive;

            await _context.SaveChangesAsync();
            Log.Information("Successfully updated permission {PermissionId}", id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(Guid id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete permission by non-admin user");
                return Forbid();
            }

            Log.Information("Attempting to delete permission {PermissionId}", id);
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                Log.Warning("Permission with ID {PermissionId} not found for deletion", id);
                return NotFound();
            }

            // Check if permission is in use
            var isInUse = await _context.RolePermissions.AnyAsync(rp => rp.PermissionId == id);
            if (isInUse)
            {
                Log.Warning("Cannot delete permission {PermissionId} as it is in use", id);
                return BadRequest("Cannot delete permission as it is currently assigned to one or more roles");
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            Log.Information("Successfully deleted permission {PermissionId}", id);
            return NoContent();
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            Log.Information("Fetching all permission categories");
            var categories = await _context.Permissions
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissionsByCategory(string category)
        {
            Log.Information("Fetching permissions for category {Category}", category);
            var permissions = await _context.Permissions
                .Where(p => p.Category == category)
                .Include(p => p.RolePermissions!)
                .ThenInclude(rp => rp.Role)
                .ToListAsync();

            return Ok(permissions);
        }
    }

    public class CreatePermissionModel
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Category { get; set; }
    }

    public class UpdatePermissionModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool? IsActive { get; set; }
    }
} 