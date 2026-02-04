using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketSalesApp.AdminServer.Models.Export;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ExportsController : ControllerBase
    {
        private readonly IExportService _exportService;
        private readonly ILogger<ExportsController> _logger;

        public ExportsController(IExportService exportService, ILogger<ExportsController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        /// <summary>
        /// Starts a new export job
        /// </summary>
        /// <param name="request">Export request parameters</param>
        /// <returns>Export job ID</returns>
        [HttpPost]
        [Authorize(Policy = "CanViewReports")]
        public async Task<ActionResult<ExportJobResponse>> StartExport([FromBody] ExportRequest request)
        {
            try
            {
                // Set the requesting user
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return BadRequest("Invalid user ID");
                }

                request.RequestedBy = userId;
                request.RequestedAt = DateTime.UtcNow;

                var jobId = await _exportService.StartExportAsync(request);

                _logger.LogInformation("Export job {JobId} started by user {UserId} for {EntityType}", 
                    jobId, userId, request.EntityType);

                return Ok(new ExportJobResponse
                {
                    JobId = jobId,
                    Message = "Export job started successfully",
                    StatusUrl = Url.Action(nameof(GetExportStatus), new { jobId })!
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid export request");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Export operation not allowed");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start export job");
                return StatusCode(500, "Failed to start export job");
            }
        }

        /// <summary>
        /// Gets the status of an export job
        /// </summary>
        /// <param name="jobId">Export job ID</param>
        /// <returns>Export status</returns>
        [HttpGet("{jobId}/status")]
        public async Task<ActionResult<ExportStatus>> GetExportStatus(string jobId)
        {
            try
            {
                var status = await _exportService.GetExportStatusAsync(jobId);
                return Ok(status);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Export job not found: {JobId}", jobId);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get export status for job {JobId}", jobId);
                return StatusCode(500, "Failed to get export status");
            }
        }

        /// <summary>
        /// Downloads the export file
        /// </summary>
        /// <param name="jobId">Export job ID</param>
        /// <returns>Export file</returns>
        [HttpGet("{jobId}/download")]
        public async Task<IActionResult> DownloadExport(string jobId)
        {
            try
            {
                var download = await _exportService.GetExportDownloadAsync(jobId);
                
                // Read the file
                var filePath = Path.Combine("exports", jobId, download.FileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Export file not found");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                
                return File(fileBytes, download.ContentType, download.FileName);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Export job not found: {JobId}", jobId);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Export download not available: {JobId}", jobId);
                return BadRequest(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Export file not found: {JobId}", jobId);
                return NotFound("Export file not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download export for job {JobId}", jobId);
                return StatusCode(500, "Failed to download export file");
            }
        }

        /// <summary>
        /// Cancels an export job
        /// </summary>
        /// <param name="jobId">Export job ID</param>
        /// <returns>Cancellation result</returns>
        [HttpPost("{jobId}/cancel")]
        public async Task<ActionResult<ExportCancelResponse>> CancelExport(string jobId)
        {
            try
            {
                var cancelled = await _exportService.CancelExportAsync(jobId);
                
                if (cancelled)
                {
                    _logger.LogInformation("Export job {JobId} cancelled by user", jobId);
                    return Ok(new ExportCancelResponse
                    {
                        JobId = jobId,
                        Cancelled = true,
                        Message = "Export job cancelled successfully"
                    });
                }
                else
                {
                    return BadRequest(new ExportCancelResponse
                    {
                        JobId = jobId,
                        Cancelled = false,
                        Message = "Export job cannot be cancelled (may be completed or already cancelled)"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel export job {JobId}", jobId);
                return StatusCode(500, "Failed to cancel export job");
            }
        }

        /// <summary>
        /// Gets supported export formats for an entity type
        /// </summary>
        /// <param name="entityType">Entity type</param>
        /// <returns>Supported formats</returns>
        [HttpGet("formats/{entityType}")]
        public async Task<ActionResult<IEnumerable<ExportFormatInfo>>> GetSupportedFormats(string entityType)
        {
            try
            {
                var formats = await _exportService.GetSupportedFormatsAsync(entityType);
                return Ok(formats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get supported formats for entity type {EntityType}", entityType);
                return StatusCode(500, "Failed to get supported formats");
            }
        }

        /// <summary>
        /// Gets available entity types for export
        /// </summary>
        /// <returns>Available entity types</returns>
        [HttpGet("entities")]
        public ActionResult<IEnumerable<ExportEntityInfo>> GetAvailableEntities()
        {
            var entities = new[]
            {
                new ExportEntityInfo { Name = "users", DisplayName = "Users", Description = "System users and their information" },
                new ExportEntityInfo { Name = "employees", DisplayName = "Employees", Description = "Employee records with job and department information" },
                new ExportEntityInfo { Name = "jobs", DisplayName = "Jobs", Description = "Job positions and descriptions" },
                new ExportEntityInfo { Name = "buses", DisplayName = "Buses", Description = "Bus fleet information" },
                new ExportEntityInfo { Name = "routes", DisplayName = "Routes", Description = "Bus routes with driver and bus assignments" },
                new ExportEntityInfo { Name = "tickets", DisplayName = "Tickets", Description = "Ticket information and pricing" },
                new ExportEntityInfo { Name = "sales", DisplayName = "Sales", Description = "Ticket sales transactions" },
                new ExportEntityInfo { Name = "maintenance", DisplayName = "Maintenance", Description = "Bus maintenance records" },
                new ExportEntityInfo { Name = "departments", DisplayName = "Departments", Description = "Organizational departments" },
                new ExportEntityInfo { Name = "routeschedules", DisplayName = "Route Schedules", Description = "Detailed route scheduling information" }
            };

            return Ok(entities);
        }
    }

    public class ExportJobResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string StatusUrl { get; set; } = string.Empty;
    }

    public class ExportCancelResponse
    {
        public string JobId { get; set; } = string.Empty;
        public bool Cancelled { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ExportEntityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}