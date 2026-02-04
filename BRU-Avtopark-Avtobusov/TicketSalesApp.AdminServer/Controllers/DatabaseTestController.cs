#if DEBUG
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Data.MongoDB;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;
using TicketSalesApp.Services.Implementations;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Development controller for testing database architecture
    /// Only available in DEBUG builds
    /// </summary>
    [ApiController]
    [Route("api/dev/database-test")]
    [AllowAnonymous] // For development testing only
    public class DatabaseTestController : ControllerBase
    {
        private readonly AppDbContext _sqlContext;
        private readonly IMongoContext _mongoContext;
        private readonly ICacheService _cacheService;
        private readonly IDataSynchronizationService _syncService;
        private readonly IDatabaseProviderFactory _providerFactory;
        private readonly ILogger<DatabaseTestController> _logger;
        private readonly IWebHostEnvironment _environment;

        public DatabaseTestController(
            AppDbContext sqlContext,
            IMongoContext mongoContext,
            ICacheService cacheService,
            IDataSynchronizationService syncService,
            IDatabaseProviderFactory providerFactory,
            ILogger<DatabaseTestController> logger,
            IWebHostEnvironment environment)
        {
            _sqlContext = sqlContext;
            _mongoContext = mongoContext;
            _cacheService = cacheService;
            _syncService = syncService;
            _providerFactory = providerFactory;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Test all database connections
        /// </summary>
        [HttpGet("test-connections")]
        public async Task<IActionResult> TestAllConnections()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoints only available in development");
            }

            var results = new Dictionary<string, object>();

            // Test SQL Database
            try
            {
                var canConnectSql = await _sqlContext.Database.CanConnectAsync();
                var userCount = await _sqlContext.Users.CountAsync();
                
                results["SQL"] = new
                {
                    CanConnect = canConnectSql,
                    UserCount = userCount,
                    Provider = _sqlContext.Database.ProviderName,
                    ConnectionString = _sqlContext.Database.GetConnectionString()
                };
            }
            catch (Exception ex)
            {
                results["SQL"] = new { Error = ex.Message };
            }

            // Test MongoDB
            try
            {
                var canConnectMongo = await _mongoContext.TestConnectionAsync();
                var mongoInfo = await _mongoContext.GetDatabaseInfoAsync();
                
                results["MongoDB"] = new
                {
                    CanConnect = canConnectMongo,
                    DatabaseInfo = mongoInfo
                };
            }
            catch (Exception ex)
            {
                results["MongoDB"] = new { Error = ex.Message };
            }

            // Test Redis Cache
            try
            {
                var cacheInfo = await _cacheService.GetCacheInfoAsync();
                results["Redis"] = cacheInfo;
            }
            catch (Exception ex)
            {
                results["Redis"] = new { Error = ex.Message };
            }

            return Ok(new
            {
                TestResults = results,
                Timestamp = DateTime.UtcNow,
                Environment = _environment.EnvironmentName
            });
        }

        /// <summary>
        /// Test repository pattern with all database providers
        /// </summary>
        [HttpPost("test-repository")]
        public async Task<IActionResult> TestRepositoryPattern()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoints only available in development");
            }

            try
            {
                // Test SQL repository using Unit of Work
                using var unitOfWork = new UnitOfWork(_sqlContext);
                var testUser = new User
                {
                    GuidId = Guid.NewGuid(),
                    Login = $"test_user_{DateTime.UtcNow.Ticks}",
                    PasswordHash = "test_hash",
                    Role = 0
                };

                await unitOfWork.Users.AddAsync(testUser);
                await unitOfWork.SaveChangesAsync();

                var retrievedUser = await unitOfWork.Users.GetByIdAsync(testUser.UserId);
                
                // Clean up
                if (retrievedUser != null)
                {
                    await unitOfWork.Users.DeleteAsync(retrievedUser);
                    await unitOfWork.SaveChangesAsync();
                }

                // Test MongoDB repository
                var userDocRepo = new MongoRepository<TicketSalesApp.Core.Data.MongoDB.Documents.UserDocument>(_mongoContext.Database);
                var testUserDoc = new TicketSalesApp.Core.Data.MongoDB.Documents.UserDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = testUser.UserId,
                    Login = testUser.Login,
                    PasswordHash = testUser.PasswordHash,
                    Role = testUser.Role,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await userDocRepo.InsertAsync(testUserDoc);
                var retrievedUserDoc = await userDocRepo.GetByIdAsync(testUserDoc.Id);
                
                // Clean up
                if (retrievedUserDoc != null)
                {
                    await userDocRepo.DeleteAsync(testUserDoc.Id);
                }

                return Ok(new
                {
                    Message = "Repository pattern test completed successfully",
                    SQLTest = new
                    {
                        UserCreated = testUser.UserId > 0,
                        UserRetrieved = retrievedUser != null,
                        UserDeleted = true
                    },
                    MongoTest = new
                    {
                        DocumentCreated = !string.IsNullOrEmpty(testUserDoc.Id),
                        DocumentRetrieved = retrievedUserDoc != null,
                        DocumentDeleted = true
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repository pattern test failed");
                return StatusCode(500, new
                {
                    Error = "Repository pattern test failed",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Test data synchronization between SQL and MongoDB
        /// </summary>
        [HttpPost("test-sync")]
        public async Task<IActionResult> TestDataSynchronization()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoints only available in development");
            }

            try
            {
                // Get initial counts
                var initialSqlCount = await _sqlContext.Users.CountAsync();
                var userCollection = _mongoContext.GetCollection<TicketSalesApp.Core.Data.MongoDB.Documents.UserDocument>();
                var initialMongoCount = await userCollection.CountDocumentsAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.UserDocument>.Filter.Empty);

                // Trigger synchronization
                await _syncService.SynchronizeEntityAsync<User>();

                // Get final counts
                var finalMongoCount = await userCollection.CountDocumentsAsync(Builders<TicketSalesApp.Core.Data.MongoDB.Documents.UserDocument>.Filter.Empty);

                // Get sync status
                var syncStatus = await _syncService.GetSynchronizationStatusAsync();

                return Ok(new
                {
                    Message = "Data synchronization test completed",
                    InitialCounts = new
                    {
                        SQL = initialSqlCount,
                        MongoDB = initialMongoCount
                    },
                    FinalCounts = new
                    {
                        SQL = initialSqlCount,
                        MongoDB = finalMongoCount
                    },
                    SynchronizationStatus = syncStatus,
                    SyncSuccessful = finalMongoCount >= initialSqlCount,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data synchronization test failed");
                return StatusCode(500, new
                {
                    Error = "Data synchronization test failed",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Test cache operations
        /// </summary>
        [HttpPost("test-cache")]
        public async Task<IActionResult> TestCacheOperations()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoints only available in development");
            }

            try
            {
                var testKey = $"test_key_{DateTime.UtcNow.Ticks}";
                var testValue = new { Message = "Test cache value", Timestamp = DateTime.UtcNow };

                // Test Set
                await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(5));

                // Test Get
                var retrievedValue = await _cacheService.GetAsync<object>(testKey);

                // Test Exists
                var exists = await _cacheService.ExistsAsync(testKey);

                // Test Remove
                await _cacheService.RemoveAsync(testKey);
                var existsAfterRemove = await _cacheService.ExistsAsync(testKey);

                // Get cache info
                var cacheInfo = await _cacheService.GetCacheInfoAsync();

                return Ok(new
                {
                    Message = "Cache operations test completed",
                    TestResults = new
                    {
                        SetSuccessful = true,
                        GetSuccessful = retrievedValue != null,
                        ExistsBeforeRemove = exists,
                        ExistsAfterRemove = existsAfterRemove,
                        RemoveSuccessful = !existsAfterRemove
                    },
                    CacheInfo = cacheInfo,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache operations test failed");
                return StatusCode(500, new
                {
                    Error = "Cache operations test failed",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Test database provider factory
        /// </summary>
        [HttpPost("test-providers")]
        public async Task<IActionResult> TestDatabaseProviders()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoints only available in development");
            }

            var results = new Dictionary<string, object>();

            // Test SQLite provider
            try
            {
                var sqliteProvider = _providerFactory.CreateProvider("SQLite", "Data Source=:memory:");
                var sqliteHealth = await sqliteProvider.GetHealthInfoAsync();
                results["SQLite"] = sqliteHealth;
            }
            catch (Exception ex)
            {
                results["SQLite"] = new { Error = ex.Message };
            }

            // Test SQL Server provider (if connection string available)
            try
            {
                var sqlServerProvider = _providerFactory.CreateProvider("SqlServer", "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true;");
                var sqlServerHealth = await sqlServerProvider.GetHealthInfoAsync();
                results["SqlServer"] = sqlServerHealth;
            }
            catch (Exception ex)
            {
                results["SqlServer"] = new { Error = ex.Message, Note = "SQL Server not available or connection failed" };
            }

            // Test PostgreSQL provider (if connection string available)
            try
            {
                var postgresProvider = _providerFactory.CreateProvider("PostgreSQL", "Host=localhost;Database=testdb;Username=test;Password=test");
                var postgresHealth = await postgresProvider.GetHealthInfoAsync();
                results["PostgreSQL"] = postgresHealth;
            }
            catch (Exception ex)
            {
                results["PostgreSQL"] = new { Error = ex.Message, Note = "PostgreSQL not available or connection failed" };
            }

            return Ok(new
            {
                Message = "Database provider factory test completed",
                ProviderTests = results,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Run comprehensive database architecture test
        /// </summary>
        [HttpPost("comprehensive-test")]
        public async Task<IActionResult> RunComprehensiveTest()
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("Debug endpoints only available in development");
            }

            var testResults = new Dictionary<string, object>();
            var overallSuccess = true;

            try
            {
                // Test 1: Connection Tests
                _logger.LogInformation("Running connection tests...");
                var connectionResult = await TestAllConnections();
                testResults["ConnectionTests"] = ((ObjectResult)connectionResult).Value;

                // Test 2: Repository Pattern Tests
                _logger.LogInformation("Running repository pattern tests...");
                var repositoryResult = await TestRepositoryPattern();
                testResults["RepositoryTests"] = ((ObjectResult)repositoryResult).Value;
                if (repositoryResult is not OkObjectResult) overallSuccess = false;

                // Test 3: Data Synchronization Tests
                _logger.LogInformation("Running data synchronization tests...");
                var syncResult = await TestDataSynchronization();
                testResults["SynchronizationTests"] = ((ObjectResult)syncResult).Value;
                if (syncResult is not OkObjectResult) overallSuccess = false;

                // Test 4: Cache Operations Tests
                _logger.LogInformation("Running cache operations tests...");
                var cacheResult = await TestCacheOperations();
                testResults["CacheTests"] = ((ObjectResult)cacheResult).Value;
                if (cacheResult is not OkObjectResult) overallSuccess = false;

                // Test 5: Database Provider Tests
                _logger.LogInformation("Running database provider tests...");
                var providerResult = await TestDatabaseProviders();
                testResults["ProviderTests"] = ((ObjectResult)providerResult).Value;

                return Ok(new
                {
                    Message = "Comprehensive database architecture test completed",
                    OverallSuccess = overallSuccess,
                    TestResults = testResults,
                    Summary = new
                    {
                        TotalTests = testResults.Count,
                        PassedTests = testResults.Values.Count(r => !r.ToString()!.Contains("Error")),
                        Environment = _environment.EnvironmentName,
                        Timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comprehensive database test failed");
                return StatusCode(500, new
                {
                    Error = "Comprehensive database test failed",
                    Message = ex.Message,
                    TestResults = testResults
                });
            }
        }
    }
}
#endif