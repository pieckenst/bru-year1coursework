using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.Services.Implementations.DatabaseProviders
{
    /// <summary>
    /// PostgreSQL database provider implementation
    /// </summary>
    public class PostgreSqlDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly Func<DbContextOptions<AppDbContext>, AppDbContext> _contextFactory;

        public PostgreSqlDatabaseProvider(string connectionString, Func<DbContextOptions<AppDbContext>, AppDbContext> contextFactory)
        {
            _connectionString = connectionString;
            _contextFactory = contextFactory;
        }

        public string ProviderName => "PostgreSQL";
        public string ConnectionString => _connectionString;

        public DbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(_connectionString);
            
            return _contextFactory(optionsBuilder.Options);
        }

        public async Task MigrateAsync()
        {
            using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var context = CreateContext();
                return await context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetVersionAsync()
        {
            try
            {
                using var context = CreateContext();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT version()";
                
                var result = await command.ExecuteScalarAsync();
                return result?.ToString() ?? "Unknown";
            }
            catch (Exception ex)
            {
                return $"Error getting version: {ex.Message}";
            }
        }

        public async Task<Dictionary<string, object>> GetHealthInfoAsync()
        {
            var healthInfo = new Dictionary<string, object>
            {
                ["Provider"] = ProviderName,
                ["ConnectionString"] = MaskConnectionString(_connectionString)
            };

            try
            {
                using var context = CreateContext();
                var canConnect = await context.Database.CanConnectAsync();
                healthInfo["CanConnect"] = canConnect;

                if (canConnect)
                {
                    var connection = context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    
                    // Get PostgreSQL version
                    using var versionCommand = connection.CreateCommand();
                    versionCommand.CommandText = "SELECT version()";
                    var version = await versionCommand.ExecuteScalarAsync();
                    healthInfo["Version"] = version?.ToString() ?? "Unknown";

                    // Get database name
                    using var dbNameCommand = connection.CreateCommand();
                    dbNameCommand.CommandText = "SELECT current_database()";
                    var dbName = await dbNameCommand.ExecuteScalarAsync();
                    healthInfo["DatabaseName"] = dbName;

                    // Get database size
                    using var sizeCommand = connection.CreateCommand();
                    sizeCommand.CommandText = "SELECT pg_size_pretty(pg_database_size(current_database())) AS size, pg_database_size(current_database()) AS size_bytes";
                    using var sizeReader = await sizeCommand.ExecuteReaderAsync();
                    if (await sizeReader.ReadAsync())
                    {
                        healthInfo["DatabaseSize"] = sizeReader["size"];
                        var sizeBytes = Convert.ToInt64(sizeReader["size_bytes"]);
                        healthInfo["DatabaseSizeBytes"] = sizeBytes;
                        healthInfo["DatabaseSizeMB"] = Math.Round(sizeBytes / 1024.0 / 1024.0, 2);
                    }
                    sizeReader.Close();

                    // Get table count
                    using var tableCommand = connection.CreateCommand();
                    tableCommand.CommandText = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_type = 'BASE TABLE'";
                    var tableCount = await tableCommand.ExecuteScalarAsync();
                    healthInfo["TableCount"] = tableCount;

                    // Get connection info
                    using var connInfoCommand = connection.CreateCommand();
                    connInfoCommand.CommandText = @"
                        SELECT 
                            inet_server_addr() AS server_address,
                            inet_server_port() AS server_port,
                            current_user AS current_user";
                    using var connInfoReader = await connInfoCommand.ExecuteReaderAsync();
                    if (await connInfoReader.ReadAsync())
                    {
                        healthInfo["ServerAddress"] = connInfoReader["server_address"] ?? "localhost";
                        healthInfo["ServerPort"] = connInfoReader["server_port"];
                        healthInfo["CurrentUser"] = connInfoReader["current_user"];
                    }
                    connInfoReader.Close();
                }
            }
            catch (Exception ex)
            {
                healthInfo["Error"] = ex.Message;
                healthInfo["CanConnect"] = false;
            }

            return healthInfo;
        }

        private static string MaskConnectionString(string connectionString)
        {
            // Mask sensitive information in connection string
            var masked = connectionString;
            
            // Mask password
            masked = System.Text.RegularExpressions.Regex.Replace(masked, @"Password=[^;]*", "Password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            masked = System.Text.RegularExpressions.Regex.Replace(masked, @"PWD=[^;]*", "PWD=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // Also handle PostgreSQL-style connection strings
            masked = System.Text.RegularExpressions.Regex.Replace(masked, @"password=[^;]*", "password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return masked;
        }
    }
}