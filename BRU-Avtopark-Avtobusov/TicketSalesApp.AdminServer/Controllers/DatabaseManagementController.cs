using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Data.MongoDB;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Controller for database management operations
    /// </summary>
    [ApiController]
    [Route("api/v1/database-management")]
    [Authorize(Policy = "AdminOnly")]
    public class DatabaseManagementController : ControllerBase
    {
        private readonly IDatabaseProviderFactory _databaseProviderFactory;
        private readonly IMongoContext _mongoContext;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DatabaseManagementController> _logger;
        private readonly IConfiguration _configuration;

        public DatabaseManagementController(
            IDatabaseProviderFactory databaseProviderFactory,
            IMongoContext mongoContext,
            ICacheService cacheService,
            ILogger<DatabaseManagementController> logger,
            IConfiguration configuration)
        {
            _databaseProviderFactory = databaseProviderFactory;
            _mongoContext = mongoContext;
            _cacheService = cacheService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get available database providers
        /// </summary>
        [HttpGet("providers")]
        public IActionResult GetAvailableProviders()
        {
            var providers = new[]
            {
                new { Name = "SQLite", Description = "Lightweight file-based database", ConnectionStringExample = "Data Source=database.db" },
                new { Name = "SqlServer", Description = "Microsoft SQL Server", ConnectionStringExample = "Server=localhost;Database=TicketSales;Trusted_Connection=true;" },
                new { Name = "PostgreSQL", Description = "PostgreSQL database", ConnectionStringExample = "Host=localhost;Database=ticketsales;Username=user;Password=password" },
                new { Name = "MongoDB", Description = "MongoDB document database", ConnectionStringExample = "mongodb://localhost:27017/ticketsales" }
            };

            return Ok(new
            {
                AvailableProviders = providers,
                CurrentProvider = _configuration.GetValue<string>("Database:Provider", "SQLite"),
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Test connection to a database provider
        /// </summary>
        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
        {
            try
            {
                if (request.Provider.ToLowerInvariant() == "mongodb")
                {
                    // Test MongoDB connection
                    var canConnect = await _mongoContext.TestConnectionAsync();
                    var dbInfo = await _mongoContext.GetDatabaseInfoAsync();
                    
                    return Ok(new
                    {
                        Provider = request.Provider,
                        CanConnect = canConnect,
                        DatabaseInfo = dbInfo,
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    // Test SQL database connection
                    var provider = _databaseProviderFactory.CreateProvider(request.Provider, request.ConnectionString);
                    var healthInfo = await provider.GetHealthInfoAsync();
                    
                    return Ok(new
                    {
                        Provider = request.Provider,
                        ConnectionString = request.ConnectionString,
                        HealthInfo = healthInfo,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test connection to {Provider}", request.Provider);
                return BadRequest(new
                {
                    Error = "Connection test failed",
                    Provider = request.Provider,
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get current database configuration
        /// </summary>
        [HttpGet("configuration")]
        public IActionResult GetDatabaseConfiguration()
        {
            var config = new
            {
                PrimaryDatabase = new
                {
                    Provider = _configuration.GetValue<string>("Database:Provider", "SQLite"),
                    ConnectionString = _configuration.GetConnectionString("DefaultConnection")
                },
                MongoDB = new
                {
                    ConnectionString = _configuration.GetConnectionString("MongoDB"),
                    Enabled = !string.IsNullOrEmpty(_configuration.GetConnectionString("MongoDB"))
                },
                Redis = new
                {
                    ConnectionString = _configuration.GetConnectionString("Redis"),
                    Enabled = !string.IsNullOrEmpty(_configuration.GetConnectionString("Redis"))
                },
                Timestamp = DateTime.UtcNow
            };

            return Ok(config);
        }

        /// <summary>
        /// Clear all caches
        /// </summary>
        [HttpPost("clear-cache")]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                await _cacheService.FlushAllAsync();
                
                return Ok(new
                {
                    Message = "All caches cleared successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cache");
                return StatusCode(500, new
                {
                    Error = "Failed to clear cache",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        [HttpGet("cache-stats")]
        public async Task<IActionResult> GetCacheStats()
        {
            try
            {
                var cacheInfo = await _cacheService.GetCacheInfoAsync();
                
                return Ok(new
                {
                    CacheInfo = cacheInfo,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache statistics");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve cache statistics",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Initialize database indexes and collections
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializeDatabase()
        {
            try
            {
                // Initialize MongoDB indexes
                await _mongoContext.CreateIndexesAsync();
                
                return Ok(new
                {
                    Message = "Database initialization completed successfully",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                return StatusCode(500, new
                {
                    Error = "Database initialization failed",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get database performance metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetDatabaseMetrics()
        {
            try
            {
                var metrics = new
                {
                    MongoDB = await GetMongoMetricsAsync(),
                    Cache = await GetCacheMetricsAsync(),
                    Timestamp = DateTime.UtcNow
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database metrics");
                return StatusCode(500, new
                {
                    Error = "Failed to retrieve database metrics",
                    Message = ex.Message
                });
            }
        }

        private async Task<object> GetMongoMetricsAsync()
        {
            try
            {
                var dbInfo = await _mongoContext.GetDatabaseInfoAsync();
                var canConnect = await _mongoContext.TestConnectionAsync();
                
                return new
                {
                    CanConnect = canConnect,
                    DatabaseInfo = dbInfo,
                    Collections = await GetMongoCollectionStatsAsync()
                };
            }
            catch (Exception ex)
            {
                return new { Error = ex.Message };
            }
        }

        private async Task<object> GetCacheMetricsAsync()
        {
            try
            {
                return await _cacheService.GetCacheInfoAsync();
            }
            catch (Exception ex)
            {
                return new { Error = ex.Message };
            }
        }

        private async Task<object> GetMongoCollectionStatsAsync()
        {
            try
            {
                var collections = new Dictionary<string, object>();
                
                // Get stats for each collection type
                var userCollection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.UserDocument>();
                collections["Users"] = await userCollection.CountDocumentsAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.UserDocument>.Filter.Empty);
                
                var busCollection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.BusDocument>();
                collections["Buses"] = await busCollection.CountDocumentsAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.BusDocument>.Filter.Empty);
                
                var routeCollection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.RouteDocument>();
                collections["Routes"] = await routeCollection.CountDocumentsAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.RouteDocument>.Filter.Empty);
                
                var ticketCollection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.TicketDocument>();
                collections["Tickets"] = await ticketCollection.CountDocumentsAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.TicketDocument>.Filter.Empty);
                
                var employeeCollection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument>();
                collections["Employees"] = await employeeCollection.CountDocumentsAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.EmployeeDocument>.Filter.Empty);
                
                return collections;
            }
            catch (Exception ex)
            {
                return new { Error = ex.Message };
            }
        }
    }

    /// <summary>
    /// Request model for testing database connections
    /// </summary>
    public class TestConnectionRequest
    {
        public string Provider { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }
}