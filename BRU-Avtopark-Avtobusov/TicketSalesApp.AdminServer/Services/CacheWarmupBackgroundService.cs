using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Background service that warms up cache on application startup
    /// </summary>
    public class CacheWarmupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheWarmupBackgroundService> _logger;

        public CacheWarmupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CacheWarmupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Wait a bit for the application to fully start
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var cacheWarmupService = scope.ServiceProvider.GetRequiredService<ICacheWarmupService>();

                _logger.LogInformation("Starting cache warmup background service");

                // Check if warmup is needed
                if (await cacheWarmupService.IsWarmupNeededAsync())
                {
                    _logger.LogInformation("Cache warmup needed, starting warmup process");
                    await cacheWarmupService.WarmupAllAsync();
                    _logger.LogInformation("Cache warmup completed successfully");
                }
                else
                {
                    _logger.LogInformation("Cache warmup not needed, cache is already populated");
                }

                // Optionally, set up periodic cache refresh
                await PeriodicCacheRefresh(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cache warmup background service was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache warmup background service");
            }
        }

        private async Task PeriodicCacheRefresh(CancellationToken stoppingToken)
        {
            // Refresh cache every hour
            var refreshInterval = TimeSpan.FromHours(1);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(refreshInterval, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var cacheWarmupService = scope.ServiceProvider.GetRequiredService<ICacheWarmupService>();

                    _logger.LogDebug("Performing periodic cache refresh");
                    await cacheWarmupService.WarmupAllAsync();
                    _logger.LogDebug("Periodic cache refresh completed");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during periodic cache refresh");
                }
            }
        }
    }
}