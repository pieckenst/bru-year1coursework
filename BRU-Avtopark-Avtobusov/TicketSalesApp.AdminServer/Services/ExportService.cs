using Hangfire;
using Microsoft.Extensions.Options;
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.AdminServer.Models.Export;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Services
{
    public class ExportService : IExportService
    {
        private readonly IExportDataProvider _dataProvider;
        private readonly IExportFileWriter _fileWriter;
        private readonly IExportProgressTracker _progressTracker;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<ExportService> _logger;
        private readonly ExportOptions _options;

        private static readonly Dictionary<ExportFormat, ExportFormatInfo> SupportedFormats = new()
        {
            {
                ExportFormat.CSV,
                new ExportFormatInfo
                {
                    Name = "csv",
                    DisplayName = "CSV (Comma Separated Values)",
                    FileExtension = ".csv",
                    ContentType = "text/csv",
                    SupportsStreaming = true,
                    MaxRecords = 1000000
                }
            },
            {
                ExportFormat.Excel,
                new ExportFormatInfo
                {
                    Name = "excel",
                    DisplayName = "Excel Workbook",
                    FileExtension = ".xlsx",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    SupportsStreaming = false,
                    MaxRecords = 100000
                }
            },
            {
                ExportFormat.JSON,
                new ExportFormatInfo
                {
                    Name = "json",
                    DisplayName = "JSON (JavaScript Object Notation)",
                    FileExtension = ".json",
                    ContentType = "application/json",
                    SupportsStreaming = true,
                    MaxRecords = 500000
                }
            }
        };

        public ExportService(
            IExportDataProvider dataProvider,
            IExportFileWriter fileWriter,
            IExportProgressTracker progressTracker,
            IBackgroundJobClient backgroundJobClient,
            IOptions<ExportOptions> options,
            ILogger<ExportService> logger)
        {
            _dataProvider = dataProvider;
            _fileWriter = fileWriter;
            _progressTracker = progressTracker;
            _backgroundJobClient = backgroundJobClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> StartExportAsync(ExportRequest request)
        {
            // Validate request
            if (!_dataProvider.IsEntityTypeSupported(request.EntityType))
            {
                throw new ArgumentException($"Entity type '{request.EntityType}' is not supported for export");
            }

            var formatInfo = SupportedFormats[request.Format];
            var totalRecords = await _dataProvider.GetTotalCountAsync(request.EntityType, request.Filters);

            if (request.MaxRecords.HasValue)
            {
                totalRecords = Math.Min(totalRecords, request.MaxRecords.Value);
            }

            if (totalRecords > formatInfo.MaxRecords)
            {
                throw new InvalidOperationException(
                    $"Export would exceed maximum records limit for {formatInfo.DisplayName} format. " +
                    $"Requested: {totalRecords}, Maximum: {formatInfo.MaxRecords}");
            }

            // Create export job
            var jobId = Guid.NewGuid().ToString();
            var fileName = request.FileName ?? GenerateFileName(request.EntityType, request.Format);
            var filePath = Path.Combine(_options.ExportDirectory, jobId, fileName);

            var exportJob = new ExportJob
            {
                JobId = jobId,
                EntityType = request.EntityType,
                Format = request.Format,
                Filters = request.Filters,
                SelectedFields = request.SelectedFields,
                MaxRecords = request.MaxRecords,
                IncludeHeaders = request.IncludeHeaders,
                FileName = fileName,
                RequestedBy = request.RequestedBy,
                RequestedAt = request.RequestedAt,
                State = ExportState.Queued,
                ExpiresAt = DateTime.UtcNow.AddHours(_options.FileExpirationHours)
            };

            await _progressTracker.SaveJobAsync(exportJob);

            // Queue background job
            var hangfireJobId = _backgroundJobClient.Enqueue<ExportBackgroundService>(
                service => service.ProcessExportAsync(jobId, CancellationToken.None));

            _logger.LogInformation("Export job {JobId} queued for {EntityType} in {Format} format. Hangfire job: {HangfireJobId}",
                jobId, request.EntityType, request.Format, hangfireJobId);

            return jobId;
        }

        public async Task<ExportStatus> GetExportStatusAsync(string jobId)
        {
            var job = await _progressTracker.GetJobAsync(jobId);
            if (job == null)
            {
                throw new ArgumentException($"Export job '{jobId}' not found");
            }

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

        public async Task<ExportDownload> GetExportDownloadAsync(string jobId)
        {
            var job = await _progressTracker.GetJobAsync(jobId);
            if (job == null)
            {
                throw new ArgumentException($"Export job '{jobId}' not found");
            }

            if (job.State != ExportState.Completed)
            {
                throw new InvalidOperationException($"Export job '{jobId}' is not completed. Current state: {job.State}");
            }

            if (string.IsNullOrEmpty(job.FilePath) || !File.Exists(job.FilePath))
            {
                throw new FileNotFoundException($"Export file for job '{jobId}' not found");
            }

            if (DateTime.UtcNow > job.ExpiresAt)
            {
                throw new InvalidOperationException($"Export file for job '{jobId}' has expired");
            }

            var downloadUrl = $"/api/v1/exports/{jobId}/download";
            var contentType = _fileWriter.GetContentType(job.Format);

            return new ExportDownload
            {
                DownloadUrl = downloadUrl,
                FileName = job.FileName ?? "export" + _fileWriter.GetFileExtension(job.Format),
                FileSizeBytes = job.FileSizeBytes ?? 0,
                ExpiresAt = job.ExpiresAt,
                ContentType = contentType
            };
        }

        public async Task<bool> CancelExportAsync(string jobId)
        {
            var job = await _progressTracker.GetJobAsync(jobId);
            if (job == null)
            {
                return false;
            }

            if (job.State == ExportState.Completed || job.State == ExportState.Failed || job.State == ExportState.Cancelled)
            {
                return false;
            }

            await _progressTracker.MarkCancelledAsync(jobId);

            _logger.LogInformation("Export job {JobId} was cancelled", jobId);
            return true;
        }

        public async Task<int> CleanupExpiredExportsAsync()
        {
            var exportDirectory = new DirectoryInfo(_options.ExportDirectory);
            if (!exportDirectory.Exists)
            {
                return 0;
            }

            var cleanedCount = 0;
            var cutoffTime = DateTime.UtcNow.AddHours(-_options.FileExpirationHours);

            foreach (var jobDirectory in exportDirectory.GetDirectories())
            {
                try
                {
                    if (jobDirectory.CreationTimeUtc < cutoffTime)
                    {
                        jobDirectory.Delete(true);
                        cleanedCount++;
                        _logger.LogDebug("Cleaned up expired export directory: {DirectoryName}", jobDirectory.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up export directory: {DirectoryName}", jobDirectory.Name);
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {CleanedCount} expired export directories", cleanedCount);
            }

            return cleanedCount;
        }

        public async Task<IEnumerable<ExportFormatInfo>> GetSupportedFormatsAsync(string entityType)
        {
            if (!_dataProvider.IsEntityTypeSupported(entityType))
            {
                return Enumerable.Empty<ExportFormatInfo>();
            }

            return await Task.FromResult(SupportedFormats.Values);
        }

        private string GenerateFileName(string entityType, ExportFormat format)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var extension = _fileWriter.GetFileExtension(format);
            return $"{entityType}_export_{timestamp}{extension}";
        }
    }

    public class ExportBackgroundService
    {
        private readonly IExportDataProvider _dataProvider;
        private readonly IExportFileWriter _fileWriter;
        private readonly IExportProgressTracker _progressTracker;
        private readonly ILogger<ExportBackgroundService> _logger;

        public ExportBackgroundService(
            IExportDataProvider dataProvider,
            IExportFileWriter fileWriter,
            IExportProgressTracker progressTracker,
            ILogger<ExportBackgroundService> logger)
        {
            _dataProvider = dataProvider;
            _fileWriter = fileWriter;
            _progressTracker = progressTracker;
            _logger = logger;
        }

        public async Task ProcessExportAsync(string jobId, CancellationToken cancellationToken)
        {
            var job = await _progressTracker.GetJobAsync(jobId);
            if (job == null)
            {
                _logger.LogError("Export job {JobId} not found", jobId);
                return;
            }

            try
            {
                _logger.LogInformation("Starting export processing for job {JobId}", jobId);

                // Get total count and mark as started
                var totalRecords = await _dataProvider.GetTotalCountAsync(job.EntityType, job.Filters);
                if (job.MaxRecords.HasValue)
                {
                    totalRecords = Math.Min(totalRecords, job.MaxRecords.Value);
                }

                await _progressTracker.MarkStartedAsync(jobId, totalRecords);

                // Create directory for the export file
                var directory = Path.GetDirectoryName(job.FilePath!)!;
                Directory.CreateDirectory(directory);

                // Get field names if not specified
                string[]? fieldNames = job.SelectedFields;
                if (fieldNames == null || !fieldNames.Any())
                {
                    var availableFields = await _dataProvider.GetAvailableFieldsAsync(job.EntityType);
                    fieldNames = availableFields.ToArray();
                }

                // Get data and write file
                var dataBatches = _dataProvider.GetDataBatchesAsync(
                    job.EntityType,
                    job.Filters,
                    fieldNames,
                    batchSize: 1000,
                    job.MaxRecords);

                // Create progress tracking wrapper
                var progressTrackingBatches = TrackProgress(dataBatches, jobId, totalRecords);

                var filePath = await _fileWriter.WriteFileAsync(
                    progressTrackingBatches,
                    job.Format,
                    job.FilePath!,
                    fieldNames,
                    job.IncludeHeaders,
                    cancellationToken);

                // Get file size and mark as completed
                var fileInfo = new FileInfo(filePath);
                await _progressTracker.MarkCompletedAsync(jobId, filePath, fileInfo.Length);

                _logger.LogInformation("Export job {JobId} completed successfully", jobId);
            }
            catch (OperationCanceledException)
            {
                await _progressTracker.MarkCancelledAsync(jobId);
                _logger.LogInformation("Export job {JobId} was cancelled", jobId);
            }
            catch (Exception ex)
            {
                await _progressTracker.MarkFailedAsync(jobId, ex.Message);
                _logger.LogError(ex, "Export job {JobId} failed", jobId);
            }
        }

        private async IAsyncEnumerable<IEnumerable<object>> TrackProgress(
            IAsyncEnumerable<IEnumerable<object>> dataBatches,
            string jobId,
            int totalRecords)
        {
            var processedRecords = 0;

            await foreach (var batch in dataBatches)
            {
                var batchList = batch.ToList();
                processedRecords += batchList.Count;

                await _progressTracker.UpdateProgressAsync(jobId, processedRecords, totalRecords);

                yield return batchList;
            }
        }
    }
}