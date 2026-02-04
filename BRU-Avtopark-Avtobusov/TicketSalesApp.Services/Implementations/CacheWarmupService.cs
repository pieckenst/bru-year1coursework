using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Service for warming up cache with frequently accessed data
    /// </summary>
    public class CacheWarmupService : ICacheWarmupService
    {
        private readonly AppDbContext _context;
        private readonly IResponseCacheService _cacheService;
        private readonly ILogger<CacheWarmupService> _logger;

        public CacheWarmupService(
            AppDbContext context,
            IResponseCacheService cacheService,
            ILogger<CacheWarmupService> logger)
        {
            _context = context;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task WarmupAllAsync()
        {
            try
            {
                _logger.LogInformation("Starting cache warmup for all data");
                
                var tasks = new[]
                {
                    WarmupBusDataAsync(),
                    WarmupRouteDataAsync(),
                    WarmupUserDataAsync(),
                    WarmupRoleDataAsync()
                };
                
                await Task.WhenAll(tasks);
                
                _logger.LogInformation("Cache warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warmup");
            }
        }

        public async Task WarmupBusDataAsync()
        {
            try
            {
                _logger.LogDebug("Warming up bus data cache");
                
                // Cache all buses (using Russian property name)
                var buses = await _context.Avtobusy.AsNoTracking().ToListAsync();
                var busListKey = _cacheService.GenerateListKey("bus");
                await _cacheService.SetAsync(busListKey, buses, null, "buses", "bus-list");
                
                // Cache individual buses
                foreach (var bus in buses)
                {
                    var busKey = _cacheService.GenerateEntityKey("bus", bus.BusId);
                    await _cacheService.SetAsync(busKey, bus, null, "buses", $"bus:{bus.BusId}");
                }
                
                _logger.LogDebug("Bus data cache warmed up with {Count} buses", buses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up bus data cache");
            }
        }

        public async Task WarmupRouteDataAsync()
        {
            try
            {
                _logger.LogDebug("Warming up route data cache");
                
                // Cache all routes (using Russian property name)
                var routes = await _context.Marshuti.AsNoTracking().ToListAsync();
                var routeListKey = _cacheService.GenerateListKey("route");
                await _cacheService.SetAsync(routeListKey, routes, null, "routes", "route-list");
                
                // Cache individual routes
                foreach (var route in routes)
                {
                    var routeKey = _cacheService.GenerateEntityKey("route", route.RouteId);
                    await _cacheService.SetAsync(routeKey, route, null, "routes", $"route:{route.RouteId}");
                }
                
                _logger.LogDebug("Route data cache warmed up with {Count} routes", routes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up route data cache");
            }
        }

        public async Task WarmupUserDataAsync()
        {
            try
            {
                _logger.LogDebug("Warming up user data cache");
                
                // Cache all users (without sensitive data)
                var users = await _context.Users
                    .AsNoTracking()
                    .Select(u => new
                    {
                        u.UserId,
                        u.Login,
                        u.Email,
                        u.Role,
                        u.IsActive,
                        u.CreatedAt,
                        u.LastLoginAt
                    })
                    .ToListAsync();
                
                var userListKey = _cacheService.GenerateListKey("user");
                await _cacheService.SetAsync(userListKey, users, null, "users", "user-list");
                
                _logger.LogDebug("User data cache warmed up with {Count} users", users.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up user data cache");
            }
        }

        public async Task WarmupRoleDataAsync()
        {
            try
            {
                _logger.LogDebug("Warming up role data cache");
                
                // Cache all roles
                var roles = await _context.Roles.AsNoTracking().ToListAsync();
                var roleListKey = _cacheService.GenerateListKey("role");
                await _cacheService.SetAsync(roleListKey, roles, null, "roles", "role-list");
                
                // Cache individual roles
                foreach (var role in roles)
                {
                    var roleKey = _cacheService.GenerateEntityKey("role", role.RoleId);
                    await _cacheService.SetAsync(roleKey, role, null, "roles", $"role:{role.RoleId}");
                }
                
                // Cache permissions
                var permissions = await _context.Permissions.AsNoTracking().ToListAsync();
                var permissionListKey = _cacheService.GenerateListKey("permission");
                await _cacheService.SetAsync(permissionListKey, permissions, null, "permissions", "permission-list");
                
                _logger.LogDebug("Role data cache warmed up with {RoleCount} roles and {PermissionCount} permissions", 
                    roles.Count, permissions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error warming up role data cache");
            }
        }

        public async Task<bool> IsWarmupNeededAsync()
        {
            try
            {
                // Check if key cache entries exist
                var busListKey = _cacheService.GenerateListKey("bus");
                var routeListKey = _cacheService.GenerateListKey("route");
                var userListKey = _cacheService.GenerateListKey("user");
                var roleListKey = _cacheService.GenerateListKey("role");
                
                var busExists = await _cacheService.GetAsync<object>(busListKey) != null;
                var routeExists = await _cacheService.GetAsync<object>(routeListKey) != null;
                var userExists = await _cacheService.GetAsync<object>(userListKey) != null;
                var roleExists = await _cacheService.GetAsync<object>(roleListKey) != null;
                
                var warmupNeeded = !busExists || !routeExists || !userExists || !roleExists;
                
                _logger.LogDebug("Cache warmup needed: {WarmupNeeded} (Bus: {BusExists}, Route: {RouteExists}, User: {UserExists}, Role: {RoleExists})",
                    warmupNeeded, busExists, routeExists, userExists, roleExists);
                
                return warmupNeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if cache warmup is needed");
                return true; // Assume warmup is needed on error
            }
        }
    }
}