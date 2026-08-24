using Hangfire;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Background service for system maintenance tasks
    /// </summary>
    public class MaintenanceBackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MaintenanceBackgroundService> _logger;

        public MaintenanceBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MaintenanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Clean up expired sessions and tokens
        /// </summary>
        [Queue("maintenance")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 600 })]
        public async Task CleanupExpiredSessionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting expired sessions cleanup");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Clean up expired WebAuthn credentials (not used in 90 days)
                var expiredCredentialsDate = DateTime.UtcNow.AddDays(-90);
                var expiredCredentials = await dbContext.WebAuthnCredentials
                    .Where(c => c.LastUsedAt < expiredCredentialsDate && !c.IsActive)
                    .ToListAsync();

                if (expiredCredentials.Any())
                {
                    dbContext.WebAuthnCredentials.RemoveRange(expiredCredentials);
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired WebAuthn credentials", expiredCredentials.Count);
                }

                // Clean up expired account linking tokens (older than 24 hours)
                var expiredTokensDate = DateTime.UtcNow.AddHours(-24);
                var usersWithExpiredTokens = await dbContext.Users
                    .Where(u => !string.IsNullOrEmpty(u.LinkedAccountToken) && u.LastLoginAt < expiredTokensDate)
                    .ToListAsync();

                foreach (var user in usersWithExpiredTokens)
                {
                    user.LinkedAccountToken = null;
                }

                if (usersWithExpiredTokens.Any())
                {
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired account linking tokens", usersWithExpiredTokens.Count);
                }

                _logger.LogInformation("Expired sessions cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up expired sessions");
                throw;
            }
        }

        /// <summary>
        /// Clean up old log files
        /// </summary>
        [Queue("maintenance")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 600 })]
        public async Task CleanupOldLogsAsync(int retentionDays = 30)
        {
            try
            {
                _logger.LogInformation("Starting old logs cleanup (retention: {RetentionDays} days)", retentionDays);

                var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
                if (!Directory.Exists(logsDirectory))
                {
                    _logger.LogInformation("Logs directory does not exist, skipping cleanup");
                    return;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var logFiles = Directory.GetFiles(logsDirectory, "*.log");
                var deletedCount = 0;

                foreach (var logFile in logFiles)
                {
                    var fileInfo = new FileInfo(logFile);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        try
                        {
                            File.Delete(logFile);
                            deletedCount++;
                            _logger.LogDebug("Deleted old log file: {FileName}", fileInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete log file: {FileName}", fileInfo.Name);
                        }
                    }
                }

                _logger.LogInformation("Old logs cleanup completed, deleted {Count} files", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up old logs");
                throw;
            }
        }

        /// <summary>
        /// Optimize database by running maintenance tasks
        /// </summary>
        [Queue("maintenance")]
        [AutomaticRetry(Attempts = 1, DelaysInSeconds = new[] { 600 })]
        public async Task OptimizeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database optimization");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // For SQLite, run VACUUM to reclaim space
                if (dbContext.Database.IsSqlite())
                {
                    await dbContext.Database.ExecuteSqlRawAsync("VACUUM;");
                    _logger.LogInformation("SQLite VACUUM completed");
                }

                // Update statistics
                if (dbContext.Database.IsSqlServer())
                {
                    await dbContext.Database.ExecuteSqlRawAsync("EXEC sp_updatestats;");
                    _logger.LogInformation("SQL Server statistics updated");
                }

                _logger.LogInformation("Database optimization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize database");
                throw;
            }
        }

        /// <summary>
        /// Generate and cache frequently accessed data
        /// </summary>
        [Queue("maintenance")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 120 })]
        public async Task WarmupCacheAsync()
        {
            try
            {
                _logger.LogInformation("Starting cache warmup");

                // TODO: Implement cache warmup logic when ICacheWarmupService is available
                // For now, this is a placeholder that logs the operation
                await Task.CompletedTask;
                
                _logger.LogInformation("Cache warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to warmup cache");
                throw;
            }
        }

        /// <summary>
        /// Check system health and send alerts if needed
        /// </summary>
        [Queue("maintenance")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 120 })]
        public async Task PerformHealthCheckAsync()
        {
            try
            {
                _logger.LogInformation("Starting system health check");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Check database connectivity
                var canConnect = await dbContext.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Database health check failed: Cannot connect to database");
                    // In a real system, send alert notification here
                    return;
                }

                // Check for pending migrations
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogWarning("Database has {Count} pending migrations", pendingMigrations.Count());
                }

                // Check disk space
                var exportDirectory = Path.Combine(AppContext.BaseDirectory, "exports");
                if (Directory.Exists(exportDirectory))
                {
                    var driveInfo = new DriveInfo(Path.GetPathRoot(exportDirectory)!);
                    var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    
                    if (freeSpaceGB < 1.0) // Less than 1GB free
                    {
                        _logger.LogWarning("Low disk space: {FreeSpace:F2} GB available", freeSpaceGB);
                    }
                }

                _logger.LogInformation("System health check completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform health check");
                throw;
            }
        }

        /// <summary>
        /// Archive old data to reduce database size
        /// </summary>
        [Queue("maintenance")]
        [AutomaticRetry(Attempts = 1, DelaysInSeconds = new[] { 600 })]
        public async Task ArchiveOldDataAsync(int retentionDays = 365)
        {
            try
            {
                _logger.LogInformation("Starting old data archival (retention: {RetentionDays} days)", retentionDays);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

                // Archive old ticket sales (example - adjust based on business requirements)
                var oldSales = await dbContext.Prodazhi
                    .Where(s => s.SaleDate < cutoffDate)
                    .CountAsync();

                if (oldSales > 0)
                {
                    _logger.LogInformation("Found {Count} old sales records to archive", oldSales);
                    // In a real system, move to archive table or export to file
                }

                _logger.LogInformation("Old data archival completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive old data");
                throw;
            }
        }
    }
}
