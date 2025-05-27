// API/Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Serilog;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAuthenticationService _authService;
        private readonly IRoleService _roleService;
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, IAuthenticationService authService, IRoleService roleService, IConfiguration configuration)
        {
            _context = context;
            _authService = authService;
            _roleService = roleService;
            _configuration = configuration;
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
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to access users list by non-admin user");
                return Forbid();
            }
            Log.Information("Fetching all users");
            var users = await _context.Users.ToListAsync();
            Log.Debug("Retrieved {UserCount} users", users.Count);
            return users;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to access user {UserId} by non-admin user", id);
                return Forbid();
            }
            Log.Information("Fetching user with ID {UserId}", id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                Log.Warning("User with ID {UserId} not found", id);
                return NotFound();
            }
            Log.Debug("Successfully retrieved user with ID {UserId}", id);
            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to create user by non-admin user");
                return Forbid();
            }

            Log.Information("Attempting to create new user with login {Login}", model.Login);
            if (await _context.Users.AnyAsync(u => u.Login == model.Login))
            {
                Log.Warning("User creation failed - login {Login} already exists", model.Login);
                return BadRequest("Login already exists");
            }

            var user = new User
            {
                Login = model.Login,
                PasswordHash = model.Password, // Will be hashed by AuthService
                Role = model.Role,
                PhoneNumber = model.PhoneNumber ?? "+375333000000",
                Email = model.Email ?? "placeholderemail@mogilev.by",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var success = await _authService.RegisterAsync(user.Login, model.Password, user.Role);
            if (!success)
            {
                Log.Error("Failed to create user with login {Login}", model.Login);
                return BadRequest("Failed to create user");
            }

            var createdUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Login == model.Login);

            if (createdUser != null)
            {
                // Update additional fields
                createdUser.PhoneNumber = user.PhoneNumber;
                createdUser.Email = user.Email;
                createdUser.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            Log.Information("Successfully created user with ID {UserId}", createdUser!.UserId);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser!.UserId }, createdUser);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update user {UserId} by non-admin user", id);
                return Forbid();
            }

            Log.Information("Attempting to update user with ID {UserId}", id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                Log.Warning("User with ID {UserId} not found for update", id);
                return NotFound();
            }

            if (!string.IsNullOrEmpty(model.Login) && model.Login != user.Login)
            {
                if (await _context.Users.AnyAsync(u => u.Login == model.Login))
                {
                    Log.Warning("Update failed - login {Login} already exists", model.Login);
                    return BadRequest("Login already exists");
                }
                Log.Information("Updating login for user {UserId} to {NewLogin}", id, model.Login);
                user.Login = model.Login;
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                Log.Information("Updating password for user {UserId}", id);
                var success = await _authService.RegisterAsync(user.Login, model.Password, user.Role);
                if (!success)
                {
                    Log.Error("Failed to update password for user {UserId}", id);
                    return BadRequest("Failed to update password");
                }
                var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                user.PasswordHash = updatedUser!.PasswordHash;
            }

            if (model.Role.HasValue)
            {
                Log.Information("Updating role for user {UserId} to {NewRole}", id, model.Role.Value);
                user.Role = model.Role.Value;
            }

            // Update new fields
            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                Log.Information("Updating phone number for user {UserId} to {NewPhone}", id, model.PhoneNumber);
                user.PhoneNumber = model.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                Log.Information("Updating email for user {UserId} to {NewEmail}", id, model.Email);
                user.Email = model.Email;
            }

            if (model.IsActive.HasValue)
            {
                Log.Information("Updating active status for user {UserId} to {IsActive}", id, model.IsActive.Value);
                user.IsActive = model.IsActive.Value;
            }

            

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("Successfully updated user with ID {UserId}", id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UserExists(id))
                {
                    Log.Warning("User with ID {UserId} not found during concurrency update", id);
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete user {UserId} by non-admin user", id);
                return Forbid();
            }

            Log.Information("Attempting to delete user with ID {UserId}", id);
            
            // Get current user ID from token
            var currentUserId = long.Parse(User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "0");
            
            // Prevent deleting yourself
            if (id == currentUserId)
            {
                Log.Warning("User {UserId} attempted to delete their own account", id);
                return BadRequest("You cannot delete your own account");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                Log.Warning("User with ID {UserId} not found for deletion", id);
                return NotFound();
            }

            // Check if this is the last admin
            if (user.Role == 1) // Admin role
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == 1);
                if (adminCount <= 1)
                {
                    Log.Warning("Attempted to delete the last admin user {UserId}", id);
                    return BadRequest("Cannot delete the last administrator account");
                }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            Log.Information("Successfully deleted user with ID {UserId}", id);
            return NoContent();
        }

        [HttpGet("{id}/roles")]
        public async Task<ActionResult<IEnumerable<Roles>>> GetUserRoles(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to access user roles for user {UserId} by non-admin user", id);
                return Forbid();
            }

            Log.Information("Fetching roles for user {UserId}", id);
            var user = await _context.Users
                .Include(u => u.UserRoles!)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                Log.Warning("User {UserId} not found while fetching roles", id);
                return NotFound();
            }

            var roles = user.UserRoles?
                .Select(ur => ur.Role!)
                .Where(r => r != null)
                .ToList() ?? new List<Roles>();

            Log.Information("Retrieved {RoleCount} roles for user {UserId}", roles.Count, id);
            return Ok(roles);
        }

        [HttpGet("{id}/permissions")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetUserPermissions(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to access user permissions for user {UserId} by non-admin user", id);
                return Forbid();
            }

            Log.Information("Fetching permissions for user {UserId}", id);
            var user = await _context.Users
                .Include(u => u.UserRoles!)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r!.RolePermissions!)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                Log.Warning("User {UserId} not found while fetching permissions", id);
                return NotFound();
            }

            var permissions = user.UserRoles?
                .SelectMany(ur => ur.Role!.RolePermissions!)
                .Select(rp => rp.Permission!)
                .Where(p => p != null)
                .Distinct()
                .ToList() ?? new List<Permission>();

            Log.Information("Retrieved {PermissionCount} permissions for user {UserId}", permissions.Count, id);
            return Ok(permissions);
        }

        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRoleToUser(long id, [FromBody] AssignRoleModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to assign role to user {UserId} by non-admin user", id);
                return Forbid();
            }

            Log.Information("Assigning role {RoleId} to user {UserId}", model.RoleId, id);
            var success = await _roleService.AssignRoleToUserAsync(id, model.RoleId);
            
            if (!success)
            {
                Log.Warning("Failed to assign role {RoleId} to user {UserId}", model.RoleId, id);
                return BadRequest("Failed to assign role to user");
            }

            Log.Information("Successfully assigned role {RoleId} to user {UserId}", model.RoleId, id);
            return NoContent();
        }

        [HttpDelete("{id}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromUser(long id, Guid roleId)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to remove role from user {UserId} by non-admin user", id);
                return Forbid();
            }

            Log.Information("Removing role {RoleId} from user {UserId}", roleId, id);
            var success = await _roleService.RemoveRoleFromUserAsync(id, roleId);
            
            if (!success)
            {
                Log.Warning("Failed to remove role {RoleId} from user {UserId}", roleId, id);
                return BadRequest("Failed to remove role from user");
            }

            Log.Information("Successfully removed role {RoleId} from user {UserId}", roleId, id);
            return NoContent();
        }

        [HttpGet("current")]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    Log.Warning("Missing or invalid Authorization header");
                    return Unauthorized(new { message = "Missing or invalid Authorization header" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var tokenHandler = new JwtSecurityTokenHandler();

                if (!tokenHandler.CanReadToken(token))
                {
                    Log.Warning("Invalid JWT token format");
                    return Unauthorized(new { message = "Invalid token format" });
                }

                // Get the JWT secret key
                var keyString = _configuration["JwtSettings:Secret"] ?? 
                    throw new InvalidOperationException("JWT secret is not configured");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

                // Set up token validation parameters
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                // Validate the token
                ClaimsPrincipal principal;
                try
                {
                    principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                    Log.Debug("Token validation successful");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Token validation failed");
                    return Unauthorized(new { message = "Invalid token", error = ex.Message });
                }

                // Get username from validated claims
                var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name) ?? 
                                   principal.Claims.FirstOrDefault(c => c.Type == "name") ??
                                   principal.Claims.FirstOrDefault(c => c.Type == "sub");

                if (usernameClaim == null)
                {
                    Log.Warning("No username claim found in validated token");
                    return Unauthorized(new { message = "Invalid token: no username claim found" });
                }

                Log.Debug("Looking up user with login: {Login}", usernameClaim.Value);
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Login == usernameClaim.Value);

                if (user == null)
                {
                    Log.Warning("User from token not found in database: {Username}", usernameClaim.Value);
                    return NotFound(new { message = $"User '{usernameClaim.Value}' not found" });
                }

                Log.Information("Successfully retrieved current user information for {Username}", user.Login);
                return user;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving current user");
                return StatusCode(500, new { message = "Internal server error while retrieving user information" });
            }
        }

        private async Task<bool> UserExists(long id)
        {
            return await _context.Users.AnyAsync(e => e.UserId == id);
        }
    }

    public class CreateUserModel
    {
        public required string Login { get; set; }
        public required string Password { get; set; }
        public int Role { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    public class UpdateUserModel
    {
        public string? Login { get; set; }
        public string? Password { get; set; }
        public int? Role { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class AssignRoleModel
    {
        public required Guid RoleId { get; set; }
    }
}
