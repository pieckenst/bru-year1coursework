using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Controller for database health monitoring and management
    /// </summary>
    [ApiController]
    [Route("api/v1/database")]
    [Authorize(Policy = "AdminOnly")]
    public class DatabaseHealthController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDatabaseProviderFactory _databaseProviderFactory;
        private readonly IDataSynchronizationService _syncService;
        private readonly ILogger<DatabaseHealthController> _logger;

        public DatabaseHealthController(
            IServiceProvider serviceProvider,
            IDatabaseProviderFactory databaseProviderFactory,
            IDataSynchronizationService syncService,
            ILogger<DatabaseHealthController> logger)
        {
            _serviceProvider = serviceProvider;
            _databaseProviderFactory = databaseProviderFactory;
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Get health status of all database connections
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetDatabaseHealth()
        {
            try
            {
                var healthInfo = await _serviceProvider.GetDatabaseHealthAsync();
                
                var overallHealth = healthInfo.ContainsKey("Error") ? "Unhealthy" : "Healthy";
                
                return Ok(new
                {
                    Status = overallHealth,
                    Timestamp = DateTime.UtcNow,
                    Databases = healthInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database health information");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "Failed to retrieve database health information",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get detailed information about the current database provider
        /// </summary>
        [HttpGet("provider")]
        public async Task<IActionResult> GetDatabaseProvider()
        {
            try
            {
                var provider = _serviceProvider.GetRequiredService<IDatabaseProvider>();
                var healthInfo = await provider.GetHealthInfoAsync();
                
                return Ok(new
                {
                    Provider = provider.GetType().Name,
                    HealthInfo = healthInfo,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database provider information");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve database provider information",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Switch to a different database provider
        /// </summary>
        [HttpPost("switch-provider")]
        public async Task<IActionResult> SwitchDatabaseProvider([FromBody] SwitchProviderRequest request)
        {
            try
            {
                // Create new provider instance
                var newProvider = _databaseProviderFactory.CreateProvider(request.Provider, request.ConnectionString);
                
                // Test connection
                var healthInfo = await newProvider.GetHealthInfoAsync();
                
                if (healthInfo.ContainsKey("CanConnect") && (bool)healthInfo["CanConnect"])
                {
                    // TODO: Implement provider switching logic
                    // This would require updating configuration and restarting services
                    
                    return Ok(new
                    {
                        Message = "Database provider switch initiated",
                        NewProvider = request.Provider,
                        HealthInfo = healthInfo,
                        Note = "Provider switching requires application restart to take full effect"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Error = "Cannot connect to new database provider",
                        Provider = request.Provider,
                        HealthInfo = healthInfo
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to switch database provider to {Provider}", request.Provider);
                return StatusCode(500, new
                {
                    Error = "Failed to switch database provider",
                    Message = ex.Message,
                    Provider = request.Provider
                });
            }
        }

        /// <summary>
        /// Get data synchronization status between SQL and MongoDB
        /// </summary>
        [HttpGet("sync/status")]
        public async Task<IActionResult> GetSynchronizationStatus()
        {
            try
            {
                var status = await _syncService.GetSynchronizationStatusAsync();
                
                return Ok(new
                {
                    SynchronizationStatus = status,
                    AutoSyncEnabled = _syncService.IsAutoSyncEnabled,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get synchronization status");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve synchronization status",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Trigger manual data synchronization
        /// </summary>
        [HttpPost("sync/trigger")]
        public async Task<IActionResult> TriggerSynchronization([FromBody] SyncRequest request)
        {
            try
            {
                if (request.SyncAll)
                {
                    await _syncService.SynchronizeAllAsync();
                    return Ok(new
                    {
                        Message = "Full data synchronization completed",
                        Timestamp = DateTime.UtcNow
                    });
                }
                else if (!string.IsNullOrEmpty(request.EntityType))
                {
                    // Synchronize specific entity type
                    switch (request.EntityType.ToLowerInvariant())
                    {
                        case "user":
                            await _syncService.SynchronizeEntityAsync<TicketSalesApp.Core.Models.User>();
                            break;
                        case "bus":
                            await _syncService.SynchronizeEntityAsync<TicketSalesApp.Core.Models.Avtobus>();
                            break;
                        case "route":
                            await _syncService.SynchronizeEntityAsync<TicketSalesApp.Core.Models.Marshut>();
                            break;
                        case "ticket":
                            await _syncService.SynchronizeEntityAsync<TicketSalesApp.Core.Models.Bilet>();
                            break;
                        case "employee":
                            await _syncService.SynchronizeEntityAsync<TicketSalesApp.Core.Models.Employee>();
                            break;
                        default:
                            return BadRequest(new { Error = $"Unknown entity type: {request.EntityType}" });
                    }
                    
                    return Ok(new
                    {
                        Message = $"Synchronization completed for entity type: {request.EntityType}",
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return BadRequest(new { Error = "Either SyncAll must be true or EntityType must be specified" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger synchronization");
                return StatusCode(500, new
                {
                    Error = "Synchronization failed",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Enable or disable automatic synchronization
        /// </summary>
        [HttpPost("sync/auto")]
        public async Task<IActionResult> SetAutoSync([FromBody] AutoSyncRequest request)
        {
            try
            {
                await _syncService.SetAutoSyncAsync(request.Enabled);
                
                return Ok(new
                {
                    Message = $"Auto-synchronization {(request.Enabled ? "enabled" : "disabled")}",
                    AutoSyncEnabled = _syncService.IsAutoSyncEnabled,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set auto-sync to {Enabled}", request.Enabled);
                return StatusCode(500, new
                {
                    Error = "Failed to set auto-synchronization",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get database connection statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDatabaseStats()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var stats = new
                {
                    Users = await context.Users.CountAsync(),
                    Employees = await context.Employees.CountAsync(),
                    Buses = await context.Avtobusy.CountAsync(),
                    Routes = await context.Marshuti.CountAsync(),
                    Tickets = await context.Bilety.CountAsync(),
                    Sales = await context.Prodazhi.CountAsync(),
                    Roles = await context.Roles.CountAsync(),
                    Permissions = await context.Permissions.CountAsync(),
                    AdminActionLogs = await context.AdminActionLogs.CountAsync(),
                    Timestamp = DateTime.UtcNow
                };
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database statistics");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve database statistics",
                    Message = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// Request model for switching database provider
    /// </summary>
    public class SwitchProviderRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for triggering synchronization
    /// </summary>
    public class SyncRequest
    {
        public bool SyncAll { get; set; }
        public string? EntityType { get; set; }
    }

    /// <summary>
    /// Request model for setting auto-sync
    /// </summary>
    public class AutoSyncRequest
    {
        public bool Enabled { get; set; }
    }
}