using Hangfire;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Configures recurring background jobs for system maintenance
    /// </summary>
    public static class ScheduledJobsConfiguration
    {
        /// <summary>
        /// Configure all recurring background jobs
        /// </summary>
        public static void ConfigureRecurringJobs()
        {
            // Export cleanup - runs every hour
            RecurringJob.AddOrUpdate<ExportCleanupService>(
                "export-cleanup",
                service => service.CleanupExpiredExports(),
                Cron.Hourly,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    QueueName = "maintenance"
                });

            // Session cleanup - runs every 6 hours
            RecurringJob.AddOrUpdate<MaintenanceBackgroundService>(
                "session-cleanup",
                service => service.CleanupExpiredSessionsAsync(),
                "0 */6 * * *", // Every 6 hours
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    QueueName = "maintenance"
                });

            // Log cleanup - runs daily at 2 AM UTC
            RecurringJob.AddOrUpdate<MaintenanceBackgroundService>(
                "log-cleanup",
                service => service.CleanupOldLogsAsync(30),
                Cron.Daily(2),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    QueueName = "maintenance"
                });

            // Database optimization - runs weekly on Sunday at 3 AM UTC
            RecurringJob.AddOrUpdate<MaintenanceBackgroundService>(
                "database-optimization",
                service => service.OptimizeDatabaseAsync(),
                Cron.Weekly(DayOfWeek.Sunday, 3),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    QueueName = "maintenance"
                });

            // Cache warmup - runs every 4 hours
            RecurringJob.AddOrUpdate<MaintenanceBackgroundService>(
                "cache-warmup",
                service => service.WarmupCacheAsync(),
                "0 */4 * * *", // Every 4 hours
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    QueueName = "maintenance"
                });

            // Health check - runs every 15 minutes
            RecurringJob.AddOrUpdate<MaintenanceBackgroundService>(
                "health-check",
                service => service.PerformHealthCheckAsync(),
                "*/15 * * * *", // Every 15 minutes
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    QueueName = "maintenance"
                });

            // Data archival - runs monthly on the 1st at 4 AM UTC
            RecurringJob.AddOrUpdate<MaintenanceBackgroundService>(
                "data-archival",
                service => service.ArchiveOldDataAsync(365),
                Cron.Monthly(1, 4),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    QueueName = "maintenance"
                });
        }

        /// <summary>
        /// Remove all recurring jobs (useful for testing or cleanup)
        /// </summary>
        public static void RemoveAllRecurringJobs()
        {
            RecurringJob.RemoveIfExists("export-cleanup");
            RecurringJob.RemoveIfExists("session-cleanup");
            RecurringJob.RemoveIfExists("log-cleanup");
            RecurringJob.RemoveIfExists("database-optimization");
            RecurringJob.RemoveIfExists("cache-warmup");
            RecurringJob.RemoveIfExists("health-check");
            RecurringJob.RemoveIfExists("data-archival");
        }
    }
}
