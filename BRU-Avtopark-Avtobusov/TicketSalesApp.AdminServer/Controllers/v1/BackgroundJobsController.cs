using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketSalesApp.AdminServer.Controllers.v1
{
    /// <summary>
    /// Controller for managing and monitoring background jobs
    /// </summary>
    [ApiController]
    [Route("api/v1/background-jobs")]
    [Authorize(Policy = "AdminOnly")]
    public class BackgroundJobsController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<BackgroundJobsController> _logger;

        public BackgroundJobsController(
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            ILogger<BackgroundJobsController> logger)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _logger = logger;
        }

        /// <summary>
        /// Get overview of all background jobs
        /// </summary>
        [HttpGet("overview")]
        public IActionResult GetJobsOverview()
        {
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var monitoring = JobStorage.Current.GetMonitoringApi();

                var statistics = monitoring.GetStatistics();

                var overview = new
                {
                    Servers = statistics.Servers,
                    Queues = statistics.Queues,
                    Enqueued = statistics.Enqueued,
                    Scheduled = statistics.Scheduled,
                    Processing = statistics.Processing,
                    Succeeded = statistics.Succeeded,
                    Failed = statistics.Failed,
                    Deleted = statistics.Deleted,
                    Recurring = statistics.Recurring,
                    Retries = statistics.Retries
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get jobs overview");
                return StatusCode(500, new { Message = "Failed to retrieve jobs overview", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of enqueued jobs
        /// </summary>
        [HttpGet("enqueued")]
        public IActionResult GetEnqueuedJobs([FromQuery] int from = 0, [FromQuery] int count = 50)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var enqueuedJobs = monitoring.EnqueuedJobs("default", from, count);

                var jobs = enqueuedJobs.Select(job => new
                {
                    JobId = job.Key,
                    job.Value.Job.Type.Name,
                    Method = job.Value.Job.Method.Name,
                    job.Value.EnqueuedAt,
                    job.Value.State
                });

                return Ok(new
                {
                    Total = monitoring.EnqueuedCount("default"),
                    From = from,
                    Count = count,
                    Jobs = jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get enqueued jobs");
                return StatusCode(500, new { Message = "Failed to retrieve enqueued jobs", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of processing jobs
        /// </summary>
        [HttpGet("processing")]
        public IActionResult GetProcessingJobs([FromQuery] int from = 0, [FromQuery] int count = 50)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var processingJobs = monitoring.ProcessingJobs(from, count);

                var jobs = processingJobs.Select(job => new
                {
                    JobId = job.Key,
                    job.Value.Job.Type.Name,
                    Method = job.Value.Job.Method.Name,
                    job.Value.StartedAt,
                    job.Value.ServerId
                });

                return Ok(new
                {
                    Total = monitoring.ProcessingCount(),
                    From = from,
                    Count = count,
                    Jobs = jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get processing jobs");
                return StatusCode(500, new { Message = "Failed to retrieve processing jobs", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of scheduled jobs
        /// </summary>
        [HttpGet("scheduled")]
        public IActionResult GetScheduledJobs([FromQuery] int from = 0, [FromQuery] int count = 50)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var scheduledJobs = monitoring.ScheduledJobs(from, count);

                var jobs = scheduledJobs.Select(job => new
                {
                    JobId = job.Key,
                    job.Value.Job.Type.Name,
                    Method = job.Value.Job.Method.Name,
                    job.Value.EnqueueAt,
                    job.Value.ScheduledAt
                });

                return Ok(new
                {
                    Total = monitoring.ScheduledCount(),
                    From = from,
                    Count = count,
                    Jobs = jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get scheduled jobs");
                return StatusCode(500, new { Message = "Failed to retrieve scheduled jobs", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of failed jobs
        /// </summary>
        [HttpGet("failed")]
        public IActionResult GetFailedJobs([FromQuery] int from = 0, [FromQuery] int count = 50)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var failedJobs = monitoring.FailedJobs(from, count);

                var jobs = failedJobs.Select(job => new
                {
                    JobId = job.Key,
                    job.Value.Job.Type.Name,
                    Method = job.Value.Job.Method.Name,
                    job.Value.FailedAt,
                    job.Value.ExceptionType,
                    job.Value.ExceptionMessage,
                    job.Value.ExceptionDetails,
                    job.Value.Reason
                });

                return Ok(new
                {
                    Total = monitoring.FailedCount(),
                    From = from,
                    Count = count,
                    Jobs = jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get failed jobs");
                return StatusCode(500, new { Message = "Failed to retrieve failed jobs", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of succeeded jobs
        /// </summary>
        [HttpGet("succeeded")]
        public IActionResult GetSucceededJobs([FromQuery] int from = 0, [FromQuery] int count = 50)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var succeededJobs = monitoring.SucceededJobs(from, count);

                var jobs = succeededJobs.Select(job => new
                {
                    JobId = job.Key,
                    job.Value.Job.Type.Name,
                    Method = job.Value.Job.Method.Name,
                    job.Value.SucceededAt,
                    job.Value.TotalDuration,
                    job.Value.Result
                });

                return Ok(new
                {
                    Total = monitoring.SucceededListCount(),
                    From = from,
                    Count = count,
                    Jobs = jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get succeeded jobs");
                return StatusCode(500, new { Message = "Failed to retrieve succeeded jobs", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get details of a specific job
        /// </summary>
        [HttpGet("{jobId}")]
        public IActionResult GetJobDetails(string jobId)
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var jobDetails = monitoring.JobDetails(jobId);

                if (jobDetails == null)
                {
                    return NotFound(new { Message = $"Job {jobId} not found" });
                }

                var details = new
                {
                    JobId = jobId,
                    jobDetails.CreatedAt,
                    jobDetails.ExpireAt,
                    Job = new
                    {
                        Type = jobDetails.Job?.Type.Name,
                        Method = jobDetails.Job?.Method.Name,
                        Arguments = jobDetails.Job?.Args
                    },
                    jobDetails.Properties,
                    History = jobDetails.History.Select(h => new
                    {
                        h.StateName,
                        h.CreatedAt,
                        h.Reason,
                        h.Data
                    })
                };

                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get job details for {JobId}", jobId);
                return StatusCode(500, new { Message = "Failed to retrieve job details", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of recurring jobs
        /// </summary>
        [HttpGet("recurring")]
        public IActionResult GetRecurringJobs()
        {
            try
            {
                using var connection = JobStorage.Current.GetConnection();
                var recurringJobs = connection.GetRecurringJobs();

                var jobs = recurringJobs.Select(job => new
                {
                    job.Id,
                    job.Cron,
                    job.TimeZoneId,
                    job.Queue,
                    job.NextExecution,
                    job.LastExecution,
                    job.LastJobId,
                    Job = new
                    {
                        Type = job.Job?.Type.Name,
                        Method = job.Job?.Method.Name
                    },
                    job.CreatedAt,
                    job.Error
                });

                return Ok(new
                {
                    Total = recurringJobs.Count,
                    Jobs = jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recurring jobs");
                return StatusCode(500, new { Message = "Failed to retrieve recurring jobs", Error = ex.Message });
            }
        }

        /// <summary>
        /// Trigger a recurring job immediately
        /// </summary>
        [HttpPost("recurring/{jobId}/trigger")]
        public IActionResult TriggerRecurringJob(string jobId)
        {
            try
            {
                _recurringJobManager.Trigger(jobId);
                _logger.LogInformation("Triggered recurring job {JobId}", jobId);
                return Ok(new { Message = $"Recurring job {jobId} triggered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger recurring job {JobId}", jobId);
                return StatusCode(500, new { Message = "Failed to trigger recurring job", Error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a job
        /// </summary>
        [HttpDelete("{jobId}")]
        public IActionResult DeleteJob(string jobId)
        {
            try
            {
                var result = _backgroundJobClient.Delete(jobId);
                if (result)
                {
                    _logger.LogInformation("Deleted job {JobId}", jobId);
                    return Ok(new { Message = $"Job {jobId} deleted successfully" });
                }
                else
                {
                    return NotFound(new { Message = $"Job {jobId} not found or already deleted" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete job {JobId}", jobId);
                return StatusCode(500, new { Message = "Failed to delete job", Error = ex.Message });
            }
        }

        /// <summary>
        /// Requeue a failed job
        /// </summary>
        [HttpPost("{jobId}/requeue")]
        public IActionResult RequeueJob(string jobId)
        {
            try
            {
                var result = _backgroundJobClient.Requeue(jobId);
                if (result)
                {
                    _logger.LogInformation("Requeued job {JobId}", jobId);
                    return Ok(new { Message = $"Job {jobId} requeued successfully" });
                }
                else
                {
                    return NotFound(new { Message = $"Job {jobId} not found or cannot be requeued" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to requeue job {JobId}", jobId);
                return StatusCode(500, new { Message = "Failed to requeue job", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get queue statistics
        /// </summary>
        [HttpGet("queues")]
        public IActionResult GetQueues()
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var queues = monitoring.Queues();

                var queueStats = queues.Select(queue => new
                {
                    queue.Name,
                    queue.Length,
                    queue.Fetched,
                    FirstJobs = monitoring.EnqueuedJobs(queue.Name, 0, 5).Select(job => new
                    {
                        JobId = job.Key,
                        job.Value.Job.Type.Name,
                        Method = job.Value.Job.Method.Name,
                        job.Value.EnqueuedAt
                    })
                });

                return Ok(new
                {
                    Total = queues.Count,
                    Queues = queueStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get queue statistics");
                return StatusCode(500, new { Message = "Failed to retrieve queue statistics", Error = ex.Message });
            }
        }

        /// <summary>
        /// Get server statistics
        /// </summary>
        [HttpGet("servers")]
        public IActionResult GetServers()
        {
            try
            {
                var monitoring = JobStorage.Current.GetMonitoringApi();
                var servers = monitoring.Servers();

                var serverStats = servers.Select(server => new
                {
                    server.Name,
                    server.Heartbeat,
                    server.WorkersCount,
                    server.Queues,
                    server.StartedAt
                });

                return Ok(new
                {
                    Total = servers.Count,
                    Servers = serverStats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get server statistics");
                return StatusCode(500, new { Message = "Failed to retrieve server statistics", Error = ex.Message });
            }
        }
    }
}
