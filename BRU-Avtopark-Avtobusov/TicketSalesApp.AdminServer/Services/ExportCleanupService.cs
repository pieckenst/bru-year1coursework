using Hangfire;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Services
{
    public class ExportCleanupService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExportCleanupService> _logger;
        private Timer? _timer;

        public ExportCleanupService(IServiceProvider serviceProvider, ILogger<ExportCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Export Cleanup Service is starting");

            // Schedule cleanup to run every hour
            RecurringJob.AddOrUpdate<ExportCleanupService>(
                "export-cleanup",
                service => service.CleanupExpiredExports(),
                Cron.Hourly);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Export Cleanup Service is stopping");
            
            _timer?.Change(Timeout.Infinite, 0);
            
            return Task.CompletedTask;
        }

        public async Task CleanupExpiredExports()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();
                
                var cleanedCount = await exportService.CleanupExpiredExportsAsync();
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {CleanedCount} expired export files", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during export cleanup");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}