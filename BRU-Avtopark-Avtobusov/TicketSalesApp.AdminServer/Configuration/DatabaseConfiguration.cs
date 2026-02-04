using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using StackExchange.Redis;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Data.MongoDB;
using TicketSalesApp.Services.Implementations;
using TicketSalesApp.Services.Implementations.DatabaseProviders;
using TicketSalesApp.Services.Interfaces;
using System.Data;

namespace TicketSalesApp.AdminServer.Configuration
{
    /// <summary>
    /// Database configuration and dependency injection setup
    /// </summary>
    public static class DatabaseConfiguration
    {
        /// <summary>
        /// Configure database services including SQL, MongoDB, and Redis
        /// This works alongside the existing AppDbContext configuration
        /// </summary>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Get database configuration
            var primaryProvider = configuration.GetValue<string>("Database:Provider") ?? 
                                configuration.GetValue<string>("DatabaseProvider", "SQLite");
            var mongoConnectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017/ticketsales";
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

            // Configure MongoDB only if it's the primary provider or explicitly enabled
            var enableMongoDB = primaryProvider.Equals("MongoDB", StringComparison.OrdinalIgnoreCase) ||
                              configuration.GetValue<bool>("Database:EnableMongoDB", false);
            
            if (enableMongoDB)
            {
                ConfigureMongoDatabase(services, mongoConnectionString);
            }
            else
            {
                // Register null MongoDB services for optional dependencies
                services.AddSingleton<IMongoClient>(provider => null!);
                services.AddSingleton<IMongoDatabase>(provider => null!);
                services.AddScoped<IMongoContext>(provider => null!);
                // Don't register the generic repository when MongoDB is disabled
            }
            
            // Configure Redis for caching
            ConfigureRedisCache(services, redisConnectionString);
            
            // Configure repository pattern and unit of work
            ConfigureRepositoryPattern(services);
            
            // Configure database providers (for future use)
            ConfigureDatabaseProviders(services);

            return services;
        }

        private static void ConfigureMongoDatabase(IServiceCollection services, string connectionString)
        {
            // Configure MongoDB client
            services.AddSingleton<IMongoClient>(provider =>
            {
                var mongoUrl = MongoUrl.Create(connectionString);
                var settings = MongoClientSettings.FromUrl(mongoUrl);
                
                // Configure connection settings
                settings.MaxConnectionPoolSize = 100;
                settings.MinConnectionPoolSize = 10;
                settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(10);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
                settings.ConnectTimeout = TimeSpan.FromSeconds(30);
                
                return new MongoClient(settings);
            });

            // Configure MongoDB database
            services.AddSingleton<IMongoDatabase>(provider =>
            {
                var client = provider.GetRequiredService<IMongoClient>();
                var mongoUrl = MongoUrl.Create(connectionString);
                return client.GetDatabase(mongoUrl.DatabaseName);
            });

            // Register MongoDB context
            services.AddScoped<IMongoContext, MongoContext>();

            // Register MongoDB repositories
            services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
        }

        private static void ConfigureRedisCache(IServiceCollection services, string connectionString)
        {
            // Configure Redis connection
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var configuration = ConfigurationOptions.Parse(connectionString);
                
                // Configure connection settings
                configuration.AbortOnConnectFail = false;
                configuration.ConnectRetry = 3;
                configuration.ConnectTimeout = 30000;
                configuration.SyncTimeout = 30000;
                configuration.AsyncTimeout = 30000;
                configuration.KeepAlive = 60;
                
                return ConnectionMultiplexer.Connect(configuration);
            });

            // Register cache service
            services.AddScoped<ICacheService, RedisCacheService>();

            // Configure distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "TicketSalesApp";
            });
        }

        private static void ConfigureRepositoryPattern(IServiceCollection services)
        {
            // Register generic repository
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            
            // Register unit of work
            services.AddScoped<IUnitOfWork>(provider =>
            {
                var context = provider.GetRequiredService<AppDbContext>();
                return new UnitOfWork(context);
            });
        }

        private static void ConfigureDatabaseProviders(IServiceCollection services)
        {
            // Register AppDbContext factory function required by DatabaseProviderFactory
            services.AddScoped<Func<DbContextOptions<AppDbContext>, AppDbContext>>(provider =>
            {
                return (options) =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var databaseProvider = configuration.GetValue<string>("DatabaseProvider", "SQLite");
                    return new AppDbContext(options, databaseProvider);
                };
            });
            
            // Register database provider factory
            services.AddScoped<IDatabaseProviderFactory, DatabaseProviderFactory>();
            
            // Register database provider based on configuration
            services.AddScoped<IDatabaseProvider>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var factory = provider.GetRequiredService<IDatabaseProviderFactory>();
                
                var databaseProvider = configuration.GetValue<string>("Database:Provider") ?? 
                                     configuration.GetValue<string>("DatabaseProvider", "SQLite");
                var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=ticketsales.db";
                
                return factory.CreateProvider(databaseProvider, connectionString);
            });
        }

        /// <summary>
        /// Initialize databases and create indexes
        /// </summary>
        public static async Task InitializeDatabasesAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            
            try
            {
                // Initialize SQL database
                var sqlContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await sqlContext.Database.MigrateAsync();

                // Apply schema updates for missing columns
                await ApplySchemaUpdatesAsync(sqlContext);

                // Initialize MongoDB indexes only if MongoDB is configured
                var mongoContext = scope.ServiceProvider.GetService<IMongoContext>();
                bool mongoCanConnect = false;
                
                if (mongoContext != null)
                {
                    try
                    {
                        await mongoContext.CreateIndexesAsync();
                        mongoCanConnect = await mongoContext.TestConnectionAsync();
                    }
                    catch (Exception ex)
                    {
                        var mongoLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                        mongoLogger.LogWarning(ex, "MongoDB initialization failed, but continuing without MongoDB support");
                    }
                }

                // Test connections
                var sqlCanConnect = await sqlContext.Database.CanConnectAsync();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Database initialization completed. SQL: {SqlStatus}, MongoDB: {MongoStatus}", 
                    sqlCanConnect ? "Connected" : "Failed", 
                    mongoContext != null ? (mongoCanConnect ? "Connected" : "Failed") : "Not Configured");
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Database initialization failed");
                throw;
            }
        }

        /// <summary>
        /// Apply schema updates for missing columns
        /// </summary>
        private static async Task ApplySchemaUpdatesAsync(AppDbContext context)
        {
            try
            {
                // Check if TOTP columns exist in Users table
                var hasIsTotpEnabled = await ColumnExistsAsync(context, "Users", "IsTotpEnabled");
                
                if (!hasIsTotpEnabled)
                {
                    // Add missing TOTP columns
                    await context.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE Users ADD COLUMN TotpSecret TEXT;
                        ALTER TABLE Users ADD COLUMN IsTotpEnabled INTEGER DEFAULT 0;
                        ALTER TABLE Users ADD COLUMN TotpEnabledAt TEXT;
                        ALTER TABLE Users ADD COLUMN TotpRecoveryCodes TEXT;
                    ");
                    
                    // Update existing users to have IsTotpEnabled = false
                    await context.Database.ExecuteSqlRawAsync("UPDATE Users SET IsTotpEnabled = 0 WHERE IsTotpEnabled IS NULL;");
                }
            }
            catch (Exception)
            {
                // Log but don't fail - the application can still work without TOTP columns
                // Silently continue if schema updates fail
            }
        }

        /// <summary>
        /// Check if a column exists in a table
        /// </summary>
        private static async Task<bool> ColumnExistsAsync(AppDbContext context, string tableName, string columnName)
        {
            try
            {
                if (context.Database.IsSqlite())
                {
                    using var command = context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = $"PRAGMA table_info({tableName})";

                    if (command.Connection!.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var colName = reader.GetString(1); // name column
                        if (string.Equals(colName, columnName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    // SQL Server implementation
                    using var command = context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName";
                    
                    var tableParam = command.CreateParameter();
                    tableParam.ParameterName = "@tableName";
                    tableParam.Value = tableName;
                    command.Parameters.Add(tableParam);

                    var columnParam = command.CreateParameter();
                    columnParam.ParameterName = "@columnName";
                    columnParam.Value = columnName;
                    command.Parameters.Add(columnParam);

                    if (command.Connection!.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch
            {
                return false; // Assume column doesn't exist if we can't check
            }
        }

        /// <summary>
        /// Get database health information
        /// </summary>
        public static async Task<Dictionary<string, object>> GetDatabaseHealthAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var healthInfo = new Dictionary<string, object>();

            try
            {
                // SQL Database health
                var sqlProvider = scope.ServiceProvider.GetRequiredService<IDatabaseProvider>();
                healthInfo["SQL"] = await sqlProvider.GetHealthInfoAsync();

                // MongoDB health (optional)
                var mongoContext = scope.ServiceProvider.GetService<IMongoContext>();
                if (mongoContext != null)
                {
                    try
                    {
                        var mongoCanConnect = await mongoContext.TestConnectionAsync();
                        var mongoInfo = await mongoContext.GetDatabaseInfoAsync();
                        healthInfo["MongoDB"] = new
                        {
                            CanConnect = mongoCanConnect,
                            Info = mongoInfo
                        };
                    }
                    catch (Exception ex)
                    {
                        healthInfo["MongoDB"] = new
                        {
                            CanConnect = false,
                            Error = ex.Message
                        };
                    }
                }
                else
                {
                    healthInfo["MongoDB"] = "Not Configured";
                }

                // Redis health
                var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                var redisDatabase = redis.GetDatabase();
                var redisCanConnect = redisDatabase.IsConnected("ping");
                healthInfo["Redis"] = new
                {
                    CanConnect = redisCanConnect,
                    IsConnected = redis.IsConnected,
                    Configuration = redis.Configuration
                };

                // Cache service health
                var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
                healthInfo["Cache"] = await cacheService.GetCacheInfoAsync();
            }
            catch (Exception ex)
            {
                healthInfo["Error"] = ex.Message;
            }

            return healthInfo;
        }
    }
}