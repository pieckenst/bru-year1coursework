using Microsoft.EntityFrameworkCore;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.Services.Implementations.DatabaseProviders
{
    /// <summary>
    /// SQLite database provider implementation
    /// </summary>
    public class SqliteDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly Func<DbContextOptions<AppDbContext>, AppDbContext> _contextFactory;

        public SqliteDatabaseProvider(string connectionString, Func<DbContextOptions<AppDbContext>, AppDbContext> contextFactory)
        {
            _connectionString = connectionString;
            _contextFactory = contextFactory;
        }

        public string ProviderName => "SQLite";
        public string ConnectionString => _connectionString;

        public DbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite(_connectionString);
            
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
                command.CommandText = "SELECT sqlite_version()";
                
                var result = await command.ExecuteScalarAsync();
                return $"SQLite {result}";
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
                    healthInfo["Version"] = await GetVersionAsync();
                    
                    var connection = context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    
                    // Get database file size if it's a file-based SQLite database
                    if (_connectionString.Contains("Data Source=") && !_connectionString.Contains(":memory:"))
                    {
                        var dataSourceStart = _connectionString.IndexOf("Data Source=") + "Data Source=".Length;
                        var dataSourceEnd = _connectionString.IndexOf(';', dataSourceStart);
                        var filePath = dataSourceEnd > 0 
                            ? _connectionString.Substring(dataSourceStart, dataSourceEnd - dataSourceStart)
                            : _connectionString.Substring(dataSourceStart);

                        if (File.Exists(filePath))
                        {
                            var fileInfo = new FileInfo(filePath);
                            healthInfo["DatabaseSizeBytes"] = fileInfo.Length;
                            healthInfo["DatabaseSizeMB"] = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2);
                        }
                    }

                    // Get table count
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                    var tableCount = await command.ExecuteScalarAsync();
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
            // For SQLite, we typically don't need to mask much since it's usually file-based
            // But we can mask any passwords if present
            return connectionString.Contains("Password=") 
                ? System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]*", "Password=***")
                : connectionString;
        }
    }
}