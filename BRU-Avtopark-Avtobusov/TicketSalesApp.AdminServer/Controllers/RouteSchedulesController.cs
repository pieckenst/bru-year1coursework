using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Serilog;
using System.Text.Json;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Allow all authenticated users to read
    public class RouteSchedulesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RouteSchedulesController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return false;

            var token = authHeader.Substring("Bearer ".Length);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
            return roleClaim?.Value == "1";
        }


        [HttpGet("pricing")]
        public async Task<ActionResult<IEnumerable<object>>> GetRoutePricing([FromQuery] long? routeId = null)
        {
            Log.Information("Fetching route pricing information with routeId filter: {RouteId}", routeId);
            
            var query = _context.RouteSchedules
                .AsNoTracking()
                .Where(rs => rs.IsActive && rs.ValidFrom <= DateTime.Now && 
                            (!rs.ValidUntil.HasValue || rs.ValidUntil.Value >= DateTime.Now));
                
            if (routeId.HasValue)
            {
                query = query.Where(rs => rs.RouteId == routeId.Value);
            }
            
            var pricingInfo = await query
                .Select(rs => new 
                {
                    rs.RouteScheduleId,
                    rs.RouteId,
                    rs.Price,
                    Currency = "BYN"
                })
                .ToListAsync();
            
            Log.Debug("Retrieved {PricingCount} pricing records", pricingInfo.Count);
            return Ok(pricingInfo);
        }
        
        [HttpGet("pricing/search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchRoutePricing(
            [FromQuery] string? startPoint = null,
            [FromQuery] string? endPoint = null)
        {
            Log.Information("Searching route pricing by startPoint: {StartPoint}, endPoint: {EndPoint}", 
                startPoint, endPoint);
            
            var query = _context.RouteSchedules
                .AsNoTracking()
                .Where(rs => rs.IsActive && rs.ValidFrom <= DateTime.Now && 
                            (!rs.ValidUntil.HasValue || rs.ValidUntil.Value >= DateTime.Now));
            
            if (!string.IsNullOrEmpty(startPoint))
            {
                query = query.Where(rs => rs.StartPoint.Contains(startPoint) || 
                                        (rs.Marshut != null && rs.Marshut.StartPoint.Contains(startPoint)));
            }
            
            if (!string.IsNullOrEmpty(endPoint))
            {
                query = query.Where(rs => rs.EndPoint.Contains(endPoint) || 
                                        (rs.Marshut != null && rs.Marshut.EndPoint.Contains(endPoint)));
            }
            
            var pricingInfo = await query
                .Select(rs => new 
                {
                    rs.RouteScheduleId,
                    rs.RouteId,
                    StartPoint = rs.StartPoint,
                    EndPoint = rs.EndPoint,
                    rs.Price,
                    Currency = "BYN"
                })
                .Take(500) // Limit results to prevent performance issues
                .ToListAsync();
            
            Log.Debug("Retrieved {PricingCount} pricing records matching search criteria", pricingInfo.Count);
            return Ok(pricingInfo);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteSchedules>>> GetRouteSchedules([FromQuery] RouteScheduleParameters parameters)
        {
            Log.Information("Fetching route schedules with pagination - Page: {PageNumber}, Size: {PageSize}, ReturnAll: {ReturnAll}", 
                parameters.PageNumber, parameters.PageSize, parameters.ReturnAll);
            
            // Use AsNoTracking to prevent EF from tracking entities when we only need to read them
            var query = _context.RouteSchedules
                .AsNoTracking()
                .Include(rs => rs.Marshut)
                .ThenInclude(m => m.Avtobus)
                .Include(rs => rs.Marshut)
                .ThenInclude(m => m.Employee)
                .OrderBy(rs => rs.DepartureTime)
                .AsQueryable();
            
            // Get total count before applying pagination
            var totalCount = await query.CountAsync();
            
            // Apply pagination unless ReturnAll is true
            var pagedSchedules = parameters.ReturnAll
                ? await query.ToListAsync()
                : await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();
            
            // Create pagination metadata
            var metadata = new
            {
                TotalCount = totalCount,
                PageSize = parameters.ReturnAll ? totalCount : parameters.PageSize,
                CurrentPage = parameters.PageNumber,
                TotalPages = parameters.ReturnAll ? 1 : (int)Math.Ceiling(totalCount / (double)parameters.PageSize),
                HasNext = !parameters.ReturnAll && parameters.PageNumber < (int)Math.Ceiling(totalCount / (double)parameters.PageSize),
                HasPrevious = !parameters.ReturnAll && parameters.PageNumber > 1,
                NextPage = !parameters.ReturnAll && parameters.PageNumber < (int)Math.Ceiling(totalCount / (double)parameters.PageSize) 
                    ? parameters.PageNumber + 1 
                    : parameters.PageNumber,
                PreviousPage = !parameters.ReturnAll && parameters.PageNumber > 1 
                    ? parameters.PageNumber - 1 
                    : 1
            };
            
            // Add pagination metadata to response headers
            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(metadata));
            
            Log.Debug("Retrieved {ScheduleCount} route schedules {PaginationInfo}", 
                pagedSchedules.Count, 
                parameters.ReturnAll ? "(all records)" : $"(page {parameters.PageNumber} of {metadata.TotalPages})");
            
            return Ok(pagedSchedules);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RouteSchedules>> GetRouteSchedule(long id)
        {
            Log.Information("Fetching route schedule with ID {ScheduleId}", id);
            
            // Use AsNoTracking for read-only operations
            var schedule = await _context.RouteSchedules
                .AsNoTracking()
                .Include(rs => rs.Marshut)
                .ThenInclude(m => m.Avtobus)
                .Include(rs => rs.Marshut)
                .ThenInclude(m => m.Employee)
                .FirstOrDefaultAsync(rs => rs.RouteScheduleId == id);

            if (schedule == null)
            {
                Log.Warning("Route schedule with ID {ScheduleId} not found", id);
                return NotFound();
            }

            Log.Debug("Successfully retrieved route schedule with ID {ScheduleId}", id);
            return schedule;
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<RouteSchedules>>> SearchRouteSchedules(
            [FromQuery] RouteScheduleSearchParameters parameters)
        {
            try
            {
                Log.Information("Searching route schedules with parameters - RouteId: {RouteId}, Date: {Date}, Day: {Day}, Active: {Active}, Page: {Page}, Size: {Size}, ReturnAll: {ReturnAll}",
                    parameters.RouteId?.ToString() ?? "any", 
                    parameters.Date?.ToString() ?? "any", 
                    parameters.DayOfWeek ?? "any", 
                    parameters.IsActive?.ToString() ?? "any",
                    parameters.PageNumber,
                    parameters.PageSize,
                    parameters.ReturnAll);

                // Use AsNoTracking to prevent entity tracking for read-only operations
                var query = _context.RouteSchedules
                    .AsNoTracking()
                    .Include(rs => rs.Marshut)
                        .ThenInclude(m => m!.Avtobus)
                    .Include(rs => rs.Marshut)
                        .ThenInclude(m => m!.Employee)
                    .AsQueryable();

                // Apply filters
                if (parameters.RouteId.HasValue)
                {
                    Log.Debug("Filtering by RouteId: {RouteId}", parameters.RouteId.Value);
                    query = query.Where(rs => rs.RouteId == parameters.RouteId.Value);
                }

                // Make date filtering very lenient
                if (parameters.Date.HasValue)
                {
                    var now = DateTime.Now.Date;
                    Log.Debug("Current date: {Now}, Target date: {Date}", now, parameters.Date.Value);
                    
                    // Check if schedule is valid (either no end date, or end date is in the future)
                    query = query.Where(rs => 
                        rs.ValidUntil == null || // No end date
                        rs.ValidUntil >= now || // Still valid
                        rs.ValidFrom <= parameters.Date.Value.AddMonths(6)); // Starting within next 6 months
                }

                if (!string.IsNullOrEmpty(parameters.DayOfWeek))
                {
                    Log.Debug("Filtering by day of week: {Day}", parameters.DayOfWeek);
                    query = query.Where(rs => rs.DaysOfWeek != null && rs.DaysOfWeek.Contains(parameters.DayOfWeek));
                }

                if (parameters.IsActive.HasValue)
                {
                    Log.Debug("Filtering by active status: {Active}", parameters.IsActive.Value);
                    query = query.Where(rs => rs.IsActive == parameters.IsActive.Value);
                }

                // Apply sorting if specified
                if (!string.IsNullOrEmpty(parameters.SortBy))
                {
                    Log.Debug("Sorting by: {SortBy}, Direction: {SortDirection}", 
                        parameters.SortBy, parameters.SortDirection);
                    
                    query = parameters.SortDirection.ToLower() == "desc" 
                        ? ApplySortingDescending(query, parameters.SortBy)
                        : ApplySortingAscending(query, parameters.SortBy);
                }
                else
                {
                    // Default sorting
                    query = query.OrderBy(rs => rs.DepartureTime);
                }

                // Get total count before applying pagination
                var totalCount = await query.CountAsync();

                // Apply pagination unless ReturnAll is true
                var pagedQuery = parameters.ReturnAll
                    ? query
                    : query.Skip((parameters.PageNumber - 1) * parameters.PageSize)
                        .Take(parameters.PageSize);

                // Use a projection to avoid circular references and ensure all properties are properly initialized
                var results = await pagedQuery
                    .Select(rs => new
                    {
                        rs.RouteScheduleId,
                        rs.RouteId,
                        rs.StartPoint,
                        rs.EndPoint,
                        RouteStops = rs.RouteStops ?? Array.Empty<string>(),
                        rs.DepartureTime,
                        rs.ArrivalTime,
                        rs.Price,
                        rs.AvailableSeats,
                        DaysOfWeek = rs.DaysOfWeek ?? Array.Empty<string>(),
                        BusTypes = rs.BusTypes ?? Array.Empty<string>(),
                        rs.IsActive,
                        rs.IsRecurring,
                        rs.ValidFrom,
                        rs.ValidUntil,
                        rs.StopDurationMinutes,
                        EstimatedStopTimes = rs.EstimatedStopTimes ?? Array.Empty<string>(),
                        StopDistances = rs.StopDistances ?? Array.Empty<double>(),
                        Notes = rs.Notes ?? string.Empty,
                        rs.CreatedAt,
                        rs.UpdatedAt,
                        UpdatedBy = rs.UpdatedBy ?? string.Empty,
                        Marshut = rs.Marshut
                    })
                    .ToListAsync();

                // Map the anonymous type back to RouteSchedules
                var schedules = results.Select(r => new RouteSchedules
                {
                    RouteScheduleId = r.RouteScheduleId,
                    RouteId = r.RouteId,
                    StartPoint = r.StartPoint,
                    EndPoint = r.EndPoint,
                    RouteStops = r.RouteStops,
                    DepartureTime = r.DepartureTime,
                    ArrivalTime = r.ArrivalTime,
                    Price = r.Price,
                    AvailableSeats = r.AvailableSeats,
                    DaysOfWeek = r.DaysOfWeek,
                    BusTypes = r.BusTypes,
                    IsActive = r.IsActive,
                    IsRecurring = r.IsRecurring,
                    ValidFrom = r.ValidFrom,
                    ValidUntil = r.ValidUntil,
                    StopDurationMinutes = r.StopDurationMinutes,
                    EstimatedStopTimes = r.EstimatedStopTimes,
                    StopDistances = r.StopDistances,
                    Notes = r.Notes,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    UpdatedBy = r.UpdatedBy,
                    Marshut = r.Marshut
                }).ToList();

                // Create pagination metadata
                var metadata = new
                {
                    TotalCount = totalCount,
                    PageSize = parameters.ReturnAll ? totalCount : parameters.PageSize,
                    CurrentPage = parameters.PageNumber,
                    TotalPages = parameters.ReturnAll ? 1 : (int)Math.Ceiling(totalCount / (double)parameters.PageSize),
                    HasNext = !parameters.ReturnAll && parameters.PageNumber < (int)Math.Ceiling(totalCount / (double)parameters.PageSize),
                    HasPrevious = !parameters.ReturnAll && parameters.PageNumber > 1,
                    NextPage = !parameters.ReturnAll && parameters.PageNumber < (int)Math.Ceiling(totalCount / (double)parameters.PageSize) 
                        ? parameters.PageNumber + 1 
                        : parameters.PageNumber,
                    PreviousPage = !parameters.ReturnAll && parameters.PageNumber > 1 
                        ? parameters.PageNumber - 1 
                        : 1,
                    FirstPage = 1,
                    LastPage = parameters.ReturnAll ? 1 : (int)Math.Ceiling(totalCount / (double)parameters.PageSize)
                };

                // Add pagination metadata to response headers
                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(metadata));

                Log.Debug("Found {Count} route schedules matching search criteria {PaginationInfo}", 
                    schedules.Count, 
                    parameters.ReturnAll ? "(all records)" : $"(page {parameters.PageNumber} of {metadata.TotalPages})");
                
                if (schedules.Count == 0 && totalCount > 0)
                {
                    // Log the actual SQL query for debugging
                    var sql = query.ToQueryString();
                    Log.Debug("SQL Query: {Sql}", sql);
                }

                return Ok(schedules);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching route schedules");
                return StatusCode(500, "An error occurred while searching route schedules. Please try again later.");
            }
        }

        private IQueryable<RouteSchedules> ApplySortingAscending(IQueryable<RouteSchedules> query, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "routeid" => query.OrderBy(rs => rs.RouteId),
                "startpoint" => query.OrderBy(rs => rs.StartPoint),
                "endpoint" => query.OrderBy(rs => rs.EndPoint),
                "departuretime" => query.OrderBy(rs => rs.DepartureTime),
                "arrivaltime" => query.OrderBy(rs => rs.ArrivalTime),
                "price" => query.OrderBy(rs => rs.Price),
                "availableseats" => query.OrderBy(rs => rs.AvailableSeats),
                "isactive" => query.OrderBy(rs => rs.IsActive),
                "validfrom" => query.OrderBy(rs => rs.ValidFrom),
                "validuntil" => query.OrderBy(rs => rs.ValidUntil),
                "createdat" => query.OrderBy(rs => rs.CreatedAt),
                "updatedat" => query.OrderBy(rs => rs.UpdatedAt),
                _ => query.OrderBy(rs => rs.DepartureTime) // Default sorting
            };
        }

        private IQueryable<RouteSchedules> ApplySortingDescending(IQueryable<RouteSchedules> query, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "routeid" => query.OrderByDescending(rs => rs.RouteId),
                "startpoint" => query.OrderByDescending(rs => rs.StartPoint),
                "endpoint" => query.OrderByDescending(rs => rs.EndPoint),
                "departuretime" => query.OrderByDescending(rs => rs.DepartureTime),
                "arrivaltime" => query.OrderByDescending(rs => rs.ArrivalTime),
                "price" => query.OrderByDescending(rs => rs.Price),
                "availableseats" => query.OrderByDescending(rs => rs.AvailableSeats),
                "isactive" => query.OrderByDescending(rs => rs.IsActive),
                "validfrom" => query.OrderByDescending(rs => rs.ValidFrom),
                "validuntil" => query.OrderByDescending(rs => rs.ValidUntil),
                "createdat" => query.OrderByDescending(rs => rs.CreatedAt),
                "updatedat" => query.OrderByDescending(rs => rs.UpdatedAt),
                _ => query.OrderByDescending(rs => rs.DepartureTime) // Default sorting
            };
        }

        [HttpPost]
        public async Task<ActionResult<RouteSchedules>> CreateRouteSchedule([FromBody] CreateRouteScheduleModel model)
        {
            try
            {
                if (!IsAdmin())
                {
                    Log.Warning("Unauthorized attempt to create route schedule by non-admin user");
                    return Forbid();
                }

                Log.Information("Creating new route schedule for route {RouteId}", model.RouteId);

                // Validate model
                if (model.RouteStops == null || model.RouteStops.Length < 2)
                {
                    Log.Warning("Invalid route stops provided: must have at least 2 stops");
                    return BadRequest("Route must have at least 2 stops");
                }

                if (model.DepartureTime >= model.ArrivalTime)
                {
                    Log.Warning("Invalid time range: departure time must be before arrival time");
                    return BadRequest("Departure time must be before arrival time");
                }

                if (model.Price <= 0)
                {
                    Log.Warning("Invalid price: must be greater than 0");
                    return BadRequest("Price must be greater than 0");
                }

                if (model.AvailableSeats <= 0)
                {
                    Log.Warning("Invalid seats: must be greater than 0");
                    return BadRequest("Available seats must be greater than 0");
                }

                var route = await _context.Marshuti.FindAsync(model.RouteId);
                if (route == null)
                {
                    Log.Warning("Invalid route ID {RouteId} provided for schedule creation", model.RouteId);
                    return BadRequest("Invalid route ID");
                }

                // Ensure arrays are initialized
                model.DaysOfWeek ??= new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                model.BusTypes ??= new[] { "МАЗ-103", "МАЗ-107" };
                model.EstimatedStopTimes ??= new string[model.RouteStops.Length];
                model.StopDistances ??= new double[model.RouteStops.Length];

                var schedule = new RouteSchedules
                {
                    RouteId = model.RouteId,
                    StartPoint = model.StartPoint,
                    EndPoint = model.EndPoint,
                    RouteStops = model.RouteStops,
                    DepartureTime = model.DepartureTime,
                    ArrivalTime = model.ArrivalTime,
                    Price = model.Price,
                    AvailableSeats = model.AvailableSeats,
                    DaysOfWeek = model.DaysOfWeek,
                    BusTypes = model.BusTypes,
                    IsActive = true,
                    ValidFrom = DateTime.Now,
                    StopDurationMinutes = model.StopDurationMinutes,
                    IsRecurring = model.IsRecurring,
                    EstimatedStopTimes = model.EstimatedStopTimes,
                    StopDistances = model.StopDistances,
                    Notes = model.Notes
                };

                _context.RouteSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                // Reload with navigation properties but use AsNoTracking to avoid duplicate tracking
                var createdSchedule = await _context.RouteSchedules
                    .AsNoTracking()
                    .Include(rs => rs.Marshut)
                    .ThenInclude(m => m!.Avtobus)
                    .Include(rs => rs.Marshut)
                    .ThenInclude(m => m!.Employee)
                    .FirstAsync(rs => rs.RouteScheduleId == schedule.RouteScheduleId);

                Log.Information("Successfully created route schedule with ID {ScheduleId}", createdSchedule.RouteScheduleId);
                return CreatedAtAction(nameof(GetRouteSchedule), new { id = createdSchedule.RouteScheduleId }, createdSchedule);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating route schedule");
                return StatusCode(500, "An error occurred while creating the route schedule. Please try again later.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRouteSchedule(long id, [FromBody] UpdateRouteScheduleModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update route schedule by non-admin user");
                return Forbid();
            }

            Log.Information("Updating route schedule with ID {ScheduleId}", id);
            var schedule = await _context.RouteSchedules.FindAsync(id);
            if (schedule == null)
            {
                Log.Warning("Route schedule with ID {ScheduleId} not found for update", id);
                return NotFound();
            }

            if (model.RouteId.HasValue)
            {
                var route = await _context.Marshuti.FindAsync(model.RouteId.Value);
                if (route == null)
                {
                    Log.Warning("Invalid route ID {RouteId} provided for schedule update", model.RouteId.Value);
                    return BadRequest("Invalid route ID");
                }
                schedule.RouteId = model.RouteId.Value;
            }

            // Update properties if provided
            if (model.StartPoint != null) schedule.StartPoint = model.StartPoint;
            if (model.EndPoint != null) schedule.EndPoint = model.EndPoint;
            if (model.RouteStops != null) schedule.RouteStops = model.RouteStops;
            if (model.DepartureTime.HasValue) schedule.DepartureTime = model.DepartureTime.Value;
            if (model.ArrivalTime.HasValue) schedule.ArrivalTime = model.ArrivalTime.Value;
            if (model.Price.HasValue) schedule.Price = model.Price.Value;
            if (model.AvailableSeats.HasValue) schedule.AvailableSeats = model.AvailableSeats.Value;
            if (model.DaysOfWeek != null) schedule.DaysOfWeek = model.DaysOfWeek;
            if (model.BusTypes != null) schedule.BusTypes = model.BusTypes;
            if (model.IsActive.HasValue) schedule.IsActive = model.IsActive.Value;
            if (model.ValidUntil.HasValue) schedule.ValidUntil = model.ValidUntil;
            if (model.StopDurationMinutes.HasValue) schedule.StopDurationMinutes = model.StopDurationMinutes.Value;
            if (model.IsRecurring.HasValue) schedule.IsRecurring = model.IsRecurring.Value;
            if (model.EstimatedStopTimes != null) schedule.EstimatedStopTimes = model.EstimatedStopTimes;
            if (model.StopDistances != null) schedule.StopDistances = model.StopDistances;
            if (model.Notes != null) schedule.Notes = model.Notes;

            schedule.UpdatedAt = DateTime.Now;
            schedule.UpdatedBy = User.Identity?.Name;

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("Successfully updated route schedule with ID {ScheduleId}", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Log.Error(ex, "Concurrency error updating route schedule with ID {ScheduleId}", id);
                if (!await RouteScheduleExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRouteSchedule(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete route schedule by non-admin user");
                return Forbid();
            }

            Log.Information("Deleting route schedule with ID {ScheduleId}", id);
            var schedule = await _context.RouteSchedules.FindAsync(id);
            if (schedule == null)
            {
                Log.Warning("Route schedule with ID {ScheduleId} not found for deletion", id);
                return NotFound();
            }

            _context.RouteSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            Log.Information("Successfully deleted route schedule with ID {ScheduleId}", id);
            return NoContent();
        }

        private async Task<bool> RouteScheduleExists(long id)
        {
            return await _context.RouteSchedules.AnyAsync(e => e.RouteScheduleId == id);
        }
    }

    // Base pagination parameters class
    public abstract class QueryStringParameters
    {
        private const int MaxPageSize = 500;
        private int _pageSize = 500;

        public int PageNumber { get; set; } = 1;
        
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        // Flag to return all results without pagination
        public bool ReturnAll { get; set; } = false;

        // Additional pagination parameters
        public string? SortBy { get; set; }
        public string SortDirection { get; set; } = "asc";
    }

    // Route schedule specific parameters
    public class RouteScheduleParameters : QueryStringParameters
    {
    }

    // Search parameters for route schedules
    public class RouteScheduleSearchParameters : QueryStringParameters
    {
        public long? RouteId { get; set; }
        public DateTime? Date { get; set; }
        public string? DayOfWeek { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateRouteScheduleModel
    {
        public required long RouteId { get; set; }
        public required string StartPoint { get; set; }
        public required string EndPoint { get; set; }
        public required string[] RouteStops { get; set; }
        public required DateTime DepartureTime { get; set; }
        public required DateTime ArrivalTime { get; set; }
        public required double Price { get; set; }
        public required int AvailableSeats { get; set; }
        public required string[] DaysOfWeek { get; set; }
        public required string[] BusTypes { get; set; }
        public int StopDurationMinutes { get; set; } = 5;
        public bool IsRecurring { get; set; } = true;
        public string[]? EstimatedStopTimes { get; set; }
        public double[]? StopDistances { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateRouteScheduleModel
    {
        public long? RouteId { get; set; }
        public string? StartPoint { get; set; }
        public string? EndPoint { get; set; }
        public string[]? RouteStops { get; set; }
        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public double? Price { get; set; }
        public int? AvailableSeats { get; set; }
        public string[]? DaysOfWeek { get; set; }
        public string[]? BusTypes { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? StopDurationMinutes { get; set; }
        public bool? IsRecurring { get; set; }
        public string[]? EstimatedStopTimes { get; set; }
        public double[]? StopDistances { get; set; }
        public string? Notes { get; set; }
    }
} 