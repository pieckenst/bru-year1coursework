using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.Services.Implementations.DatabaseProviders
{
    /// <summary>
    /// SQL Server database provider implementation
    /// </summary>
    public class SqlServerDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly Func<DbContextOptions<AppDbContext>, AppDbContext> _contextFactory;

        public SqlServerDatabaseProvider(string connectionString, Func<DbContextOptions<AppDbContext>, AppDbContext> contextFactory)
        {
            _connectionString = connectionString;
            _contextFactory = contextFactory;
        }

        public string ProviderName => "SqlServer";
        public string ConnectionString => _connectionString;

        public DbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            
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
                command.CommandText = "SELECT @@VERSION";
                
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
                    
                    // Get SQL Server version
                    using var versionCommand = connection.CreateCommand();
                    versionCommand.CommandText = "SELECT SERVERPROPERTY('ProductVersion') AS Version, SERVERPROPERTY('Edition') AS Edition";
                    using var versionReader = await versionCommand.ExecuteReaderAsync();
                    if (await versionReader.ReadAsync())
                    {
                        healthInfo["Version"] = versionReader["Version"];
                        healthInfo["Edition"] = versionReader["Edition"];
                    }
                    versionReader.Close();

                    // Get database name
                    using var dbNameCommand = connection.CreateCommand();
                    dbNameCommand.CommandText = "SELECT DB_NAME()";
                    var dbName = await dbNameCommand.ExecuteScalarAsync();
                    healthInfo["DatabaseName"] = dbName;

                    // Get database size
                    using var sizeCommand = connection.CreateCommand();
                    sizeCommand.CommandText = @"
                        SELECT 
                            SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) * 8192.) / 1024 / 1024 AS DatabaseSizeMB
                        FROM sys.database_files 
                        WHERE type IN (0,1)";
                    var sizeResult = await sizeCommand.ExecuteScalarAsync();
                    if (sizeResult != DBNull.Value && sizeResult != null)
                    {
                        healthInfo["DatabaseSizeMB"] = Math.Round(Convert.ToDouble(sizeResult), 2);
                    }

                    // Get table count
                    using var tableCommand = connection.CreateCommand();
                    tableCommand.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                    var tableCount = await tableCommand.ExecuteScalarAsync();
                    healthInfo["TableCount"] = tableCount;
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
            
            // Mask user ID if needed (optional, depending on security requirements)
            // masked = System.Text.RegularExpressions.Regex.Replace(masked, @"User ID=[^;]*", "User ID=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return masked;
        }
    }
}