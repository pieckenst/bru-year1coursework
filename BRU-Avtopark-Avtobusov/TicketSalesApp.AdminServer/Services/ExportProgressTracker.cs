using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using TicketSalesApp.AdminServer.Hubs;
using TicketSalesApp.AdminServer.Models.Export;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Services
{
    public class ExportProgressTracker : IExportProgressTracker
    {
        private readonly IDistributedCache _cache;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ExportProgressTracker> _logger;
        private const string CACHE_KEY_PREFIX = "export_job:";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

        public ExportProgressTracker(
            IDistributedCache cache,
            IHubContext<NotificationHub> hubContext,
            ILogger<ExportProgressTracker> logger)
        {
            _cache = cache;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task UpdateProgressAsync(string jobId, int processedRecords, int totalRecords)
        {
            var job = await GetJobAsync(jobId);
            if (job == null) return;

            job.ProcessedRecords = processedRecords;
            job.TotalRecords = totalRecords;

            await SaveJobAsync(job);

            var status = CreateExportStatus(job);
            await NotifyProgressAsync(jobId, status, job.RequestedBy);

            _logger.LogDebug("Export progress updated for job {JobId}: {ProcessedRecords}/{TotalRecords}", 
                jobId, processedRecords, totalRecords);
        }

        public async Task MarkStartedAsync(string jobId, int totalRecords)
        {
            var job = await GetJobAsync(jobId);
            if (job == null) return;

            job.State = ExportState.Processing;
            job.StartedAt = DateTime.UtcNow;
            job.TotalRecords = totalRecords;
            job.ProcessedRecords = 0;

            await SaveJobAsync(job);

            var status = CreateExportStatus(job);
            await NotifyProgressAsync(jobId, status, job.RequestedBy);

            _logger.LogInformation("Export job {JobId} started with {TotalRecords} records", jobId, totalRecords);
        }

        public async Task MarkCompletedAsync(string jobId, string filePath, long fileSizeBytes)
        {
            var job = await GetJobAsync(jobId);
            if (job == null) return;

            job.State = ExportState.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.FilePath = filePath;
            job.FileSizeBytes = fileSizeBytes;

            await SaveJobAsync(job);

            var status = CreateExportStatus(job);
            await NotifyProgressAsync(jobId, status, job.RequestedBy);

            _logger.LogInformation("Export job {JobId} completed successfully. File: {FilePath}, Size: {FileSizeBytes} bytes", 
                jobId, filePath, fileSizeBytes);
        }

        public async Task MarkFailedAsync(string jobId, string errorMessage)
        {
            var job = await GetJobAsync(jobId);
            if (job == null) return;

            job.State = ExportState.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = errorMessage;

            await SaveJobAsync(job);

            var status = CreateExportStatus(job);
            await NotifyProgressAsync(jobId, status, job.RequestedBy);

            _logger.LogError("Export job {JobId} failed: {ErrorMessage}", jobId, errorMessage);
        }

        public async Task MarkCancelledAsync(string jobId)
        {
            var job = await GetJobAsync(jobId);
            if (job == null) return;

            job.State = ExportState.Cancelled;
            job.CompletedAt = DateTime.UtcNow;

            await SaveJobAsync(job);

            var status = CreateExportStatus(job);
            await NotifyProgressAsync(jobId, status, job.RequestedBy);

            _logger.LogInformation("Export job {JobId} was cancelled", jobId);
        }

        public async Task NotifyProgressAsync(string jobId, ExportStatus status, Guid userId)
        {
            try
            {
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ExportProgress", new
                    {
                        JobId = jobId,
                        Status = status,
                        Timestamp = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send export progress notification for job {JobId} to user {UserId}", 
                    jobId, userId);
            }
        }

        public async Task<ExportJob?> GetJobAsync(string jobId)
        {
            try
            {
                var cacheKey = CACHE_KEY_PREFIX + jobId;
                var jobJson = await _cache.GetStringAsync(cacheKey);
                
                if (string.IsNullOrEmpty(jobJson))
                    return null;

                return JsonSerializer.Deserialize<ExportJob>(jobJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve export job {JobId} from cache", jobId);
                return null;
            }
        }

        public async Task SaveJobAsync(ExportJob job)
        {
            try
            {
                var cacheKey = CACHE_KEY_PREFIX + job.JobId;
                var jobJson = JsonSerializer.Serialize(job);
                
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiration
                };

                await _cache.SetStringAsync(cacheKey, jobJson, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save export job {JobId} to cache", job.JobId);
                throw;
            }
        }

        private ExportStatus CreateExportStatus(ExportJob job)
        {
            return new ExportStatus
            {
                JobId = job.JobId,
                State = job.State,
                TotalRecords = job.TotalRecords,
                ProcessedRecords = job.ProcessedRecords,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ErrorMessage = job.ErrorMessage,
                FileName = job.FileName,
                FileSizeBytes = job.FileSizeBytes
            };
        }
    }
}