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
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.AdminServer.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : BaseAuthorizedController
    {
        private readonly AppDbContext _context;
        private readonly IAuthenticationService _authService;
        private readonly IRoleService _roleService;
        private readonly IConfiguration _configuration;

        public UsersController(
            AppDbContext context, 
            IAuthenticationService authService, 
            IRoleService roleService, 
            IConfiguration configuration,
            ILogger<UsersController> logger,
            IRoleCacheService roleCacheService) 
            : base(logger, roleCacheService)
        {
            _context = context;
            _authService = authService;
            _roleService = roleService;
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            _logger.LogInformation("Fetching all users");
            
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    u.UserId,
                    u.GuidId,
                    u.Login,
                    u.PhoneNumber,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    u.LastLoginAt,
                    u.IsActive,
                    u.WindowsIdentity,
                    u.IsWindowsAuth,
                    u.DoesWindowsAccountNeedLinking,
                    u.LinkedRegularAccountUsername,
                    UserRoles = u.UserRoles.Select(ur => ur.Role).ToList()
                })
                .ToListAsync();
                
            _logger.LogDebug("Retrieved {UserCount} users", users.Count);
            LogAuthorizedAction("view users", new { Count = users.Count });
            return Ok(users);
        }


        [HttpGet("api-stats")]
        [AllowAnonymous]
        public IActionResult GetApiStats()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var stats = new
            {
                ServerTime = DateTime.UtcNow,
                ProcessStartTime = process.StartTime.ToUniversalTime(),
                MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
                ThreadCount = process.Threads.Count,
                CpuTime = process.TotalProcessorTime,
                UserProcessorTime = process.UserProcessorTime,
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                Is64BitProcess = Environment.Is64BitProcess
            };

            return Ok(stats);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<ActionResult<object>> GetUser(long id)
        {
            _logger.LogInformation("Fetching user with ID {UserId}", id);
            
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Select(u => new
                {
                    u.UserId,
                    u.GuidId,
                    u.Login,
                    u.PhoneNumber,
                    u.Email,
                    u.Role,
                    u.CreatedAt,
                    u.LastLoginAt,
                    u.IsActive,
                    u.WindowsIdentity,
                    u.IsWindowsAuth,
                    u.DoesWindowsAccountNeedLinking,
                    u.LinkedRegularAccountUsername,
                    UserRoles = u.UserRoles.Select(ur => ur.Role).ToList()
                })
                .FirstOrDefaultAsync(u => u.UserId == id);
                
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", id);
                return NotFound(new { Message = "User not found", Id = id });
            }
            
            _logger.LogDebug("Successfully retrieved user with ID {UserId}", id);
            LogAuthorizedAction("view user", new { UserId = id });
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserModel model)
        {
            _logger.LogInformation("Attempting to create new user with login {Login}", model.Login);
            if (await _context.Users.AnyAsync(u => u.Login == model.Login))
            {
                _logger.LogWarning("User creation failed - login {Login} already exists", model.Login);
                return BadRequest(new { Message = "Login already exists" });
            }

            var user = new User
            {
                Login = model.Login,
                PasswordHash = model.Password, // Will be hashed by AuthService
                Role = model.Role,
                PhoneNumber = model.PhoneNumber ?? "+375333000000",
                Email = model.Email ?? "placeholderemail@mogilev.by",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsWindowsAuth = model.IsWindowsAuth,
                WindowsIdentity = model.WindowsIdentity,
                DoesWindowsAccountNeedLinking = model.IsWindowsAuth, // If it's a Windows auth user, it needs linking by default
                LinkedRegularAccountUsername = model.LinkedRegularAccountUsername ?? string.Empty,
                LinkedAccountToken = model.IsWindowsAuth ? 
                    $"{Guid.NewGuid().ToString("N").Substring(0, 8)}-{model.Login?.Substring(0, Math.Min(3, model.Login?.Length ?? 0))}" : 
                    string.Empty
            };

            var success = await _authService.RegisterAsync(user.Login, model.Password, user.Role);
            if (!success)
            {
                _logger.LogError("Failed to create user with login {Login}", model.Login);
                return BadRequest(new { Message = "Failed to create user" });
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
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserModel model)
        {
            _logger.LogInformation("Attempting to update user with ID {UserId}", id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for update", id);
                return CreateNotFoundResponse("User", id);
            }

            if (!string.IsNullOrEmpty(model.Login) && model.Login != user.Login)
            {
                if (await _context.Users.AnyAsync(u => u.Login == model.Login))
                {
                    _logger.LogWarning("Update failed - login {Login} already exists", model.Login);
                    return CreateValidationErrorResponse("Login already exists");
                }
                _logger.LogInformation("Updating login for user {UserId} to {NewLogin}", id, model.Login);
                user.Login = model.Login;
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                _logger.LogInformation("Updating password for user {UserId}", id);
                var success = await _authService.RegisterAsync(user.Login, model.Password, user.Role);
                if (!success)
                {
                    _logger.LogError("Failed to update password for user {UserId}", id);
                    return CreateValidationErrorResponse("Failed to update password");
                }
                var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                user.PasswordHash = updatedUser!.PasswordHash;
            }

            if (model.Role.HasValue)
            {
                _logger.LogInformation("Updating role for user {UserId} to {NewRole}", id, model.Role.Value);
                user.Role = model.Role.Value;
            }

            // Update new fields
            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                _logger.LogInformation("Updating phone number for user {UserId} to {NewPhone}", id, model.PhoneNumber);
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

            // Update Windows account information
            if (model.IsWindowsAuth.HasValue)
            {
                Log.Information("Updating Windows auth status for user {UserId} to {IsWindowsAuth}", id, model.IsWindowsAuth.Value);
                user.IsWindowsAuth = model.IsWindowsAuth.Value;
                
                // If enabling Windows auth and no identity is set yet, use the login as identity
                if (model.IsWindowsAuth.Value && string.IsNullOrEmpty(user.WindowsIdentity))
                {
                    user.WindowsIdentity = user.Login;
                }
            }

            if (!string.IsNullOrEmpty(model.WindowsIdentity))
            {
                Log.Information("Updating Windows identity for user {UserId}", id);
                user.WindowsIdentity = model.WindowsIdentity;
            }

            if (model.DoesWindowsAccountNeedLinking.HasValue)
            {
                Log.Information("Updating Windows account linking status for user {UserId} to {NeedsLinking}", 
                    id, model.DoesWindowsAccountNeedLinking.Value);
                user.DoesWindowsAccountNeedLinking = model.DoesWindowsAccountNeedLinking.Value;
            }

            if (!string.IsNullOrEmpty(model.LinkedRegularAccountUsername))
            {
                Log.Information("Updating linked regular account for Windows user {UserId} to {LinkedAccount}", 
                    id, model.LinkedRegularAccountUsername);
                user.LinkedRegularAccountUsername = model.LinkedRegularAccountUsername;
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
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<IActionResult> DeleteUser(long id)
        {
            _logger.LogInformation("Attempting to delete user with ID {UserId}", id);
            
            // Get current user ID from token
            var currentUserId = GetCurrentUserId();
            
            // Prevent deleting yourself
            if (id == currentUserId)
            {
                _logger.LogWarning("User {UserId} attempted to delete their own account", id);
                return CreateValidationErrorResponse("You cannot delete your own account");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for deletion", id);
                return CreateNotFoundResponse("User", id);
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
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<ActionResult<IEnumerable<Roles>>> GetUserRoles(long id)
        {
            _logger.LogInformation("Fetching roles for user {UserId}", id);
            var user = await _context.Users
                .Include(u => u.UserRoles!)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found while fetching roles", id);
                return NotFound(new { Message = "User not found", Id = id });
            }

            var roles = user.UserRoles?
                .Select(ur => ur.Role!)
                .Where(r => r != null)
                .ToList() ?? new List<Roles>();

            _logger.LogInformation("Retrieved {RoleCount} roles for user {UserId}", roles.Count, id);
            LogAuthorizedAction("view user roles", new { UserId = id, RoleCount = roles.Count });
            return Ok(roles);
        }

        [HttpGet("{id}/permissions")]
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<ActionResult<IEnumerable<Permission>>> GetUserPermissions(long id)
        {
            _logger.LogInformation("Fetching permissions for user {UserId}", id);
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
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<IActionResult> AssignRoleToUser(long id, [FromBody] AssignRoleModel model)
        {
            _logger.LogInformation("Assigning role {RoleId} to user {UserId}", model.RoleId, id);
            var success = await _roleService.AssignRoleToUserAsync(id, model.RoleId);
            
            if (!success)
            {
                _logger.LogWarning("Failed to assign role {RoleId} to user {UserId}", model.RoleId, id);
                return CreateValidationErrorResponse("Failed to assign role to user");
            }

            _logger.LogInformation("Successfully assigned role {RoleId} to user {UserId}", model.RoleId, id);
            LogAuthorizedAction("assign role to user", new { UserId = id, RoleId = model.RoleId });
            return NoContent();
        }

        [HttpDelete("{id}/roles/{roleId}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageUsers)] // Admin-only operation
        public async Task<IActionResult> RemoveRoleFromUser(long id, Guid roleId)
        {
            _logger.LogInformation("Removing role {RoleId} from user {UserId}", roleId, id);
            var success = await _roleService.RemoveRoleFromUserAsync(id, roleId);
            
            if (!success)
            {
                _logger.LogWarning("Failed to remove role {RoleId} from user {UserId}", roleId, id);
                return CreateValidationErrorResponse("Failed to remove role from user");
            }

            _logger.LogInformation("Successfully removed role {RoleId} from user {UserId}", roleId, id);
            LogAuthorizedAction("remove role from user", new { UserId = id, RoleId = roleId });
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
        public bool IsWindowsAuth { get; set; } = false;
        public string? WindowsIdentity { get; set; }
        public string? LinkedRegularAccountUsername { get; set; }
    }

    public class UpdateUserModel
    {
        public string? Login { get; set; }
        public string? Password { get; set; }
        public int? Role { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsWindowsAuth { get; set; }
        public string? WindowsIdentity { get; set; }
        public bool? DoesWindowsAccountNeedLinking { get; set; }
        public string? LinkedRegularAccountUsername { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class AssignRoleModel
    {
        public required Guid RoleId { get; set; }
    }
}
