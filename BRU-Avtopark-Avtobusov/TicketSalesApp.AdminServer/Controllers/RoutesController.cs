using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using System.Linq;
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : BaseAuthorizedController
    {
        private readonly AppDbContext _context;

        public RoutesController(
            AppDbContext context,
            ILogger<RoutesController> logger,
            IRoleCacheService roleCacheService)
            : base(logger, roleCacheService)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CanViewDashboard)] // Any authenticated user can view routes
        public async Task<ActionResult<IEnumerable<Marshut>>> GetRoutes()
        {
            _logger.LogInformation("Fetching all routes with their related data");
            var routes = await _context.Marshuti
                .Include(r => r.Avtobus)
                .Include(r => r.Employee)
                .Include(r => r.Tickets)
                .ToListAsync();
            
            _logger.LogDebug("Retrieved {RouteCount} routes", routes.Count);
            LogAuthorizedAction("view routes", new { Count = routes.Count });
            return routes;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.CanViewDashboard)] // Any authenticated user can view a specific route
        public async Task<ActionResult<Marshut>> GetRoute(long id)
        {
            _logger.LogInformation("Fetching route with ID {RouteId}", id);
            var route = await _context.Marshuti
                .Include(r => r.Avtobus)
                .Include(r => r.Employee)
                .Include(r => r.Tickets)
                .FirstOrDefaultAsync(r => r.RouteId == id);

            if (route == null)
            {
                _logger.LogWarning("Route with ID {RouteId} not found", id);
                return NotFound(new { Message = "Route not found", Id = id });
            }

            _logger.LogDebug("Successfully retrieved route with ID {RouteId}", id);
            LogAuthorizedAction("view route", new { RouteId = id });
            return route;
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanManageRoutes)] // Requires route management permission
        public async Task<ActionResult<Marshut>> CreateRoute([FromBody] CreateRouteModel model)
        {
            if (model == null)
            {
                _logger.LogWarning("Route model is required");
                return BadRequest(new { Message = "Route model is required" });
            }

            if (string.IsNullOrWhiteSpace(model.StartPoint) || string.IsNullOrWhiteSpace(model.EndPoint))
            {
                _logger.LogWarning("Start point and end point are required");
                return BadRequest(new { Message = "Start point and end point are required" });
            }

            _logger.LogInformation("Creating new route from {StartPoint} to {EndPoint}", model.StartPoint, model.EndPoint);

            // Validate bus exists
            var bus = await _context.Avtobusy.FindAsync(model.BusId);
            if (bus == null)
            {
                _logger.LogWarning("Invalid bus ID {BusId} provided", model.BusId);
                return BadRequest(new { Message = "Invalid bus ID", BusId = model.BusId });
            }

            // Validate driver exists
            var driver = await _context.Employees.FindAsync(model.DriverId);
            if (driver == null)
            {
                _logger.LogWarning("Invalid driver ID {DriverId} provided", model.DriverId);
                return BadRequest(new { Message = "Invalid driver ID", DriverId = model.DriverId });
            }
            
            var route = new Marshut
            {
                StartPoint = model.StartPoint,
                EndPoint = model.EndPoint,
                BusId = model.BusId,
                DriverId = model.DriverId,
                TravelTime = model.TravelTime
            };

            _context.Marshuti.Add(route);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            route = await _context.Marshuti
                .Include(r => r.Avtobus)
                .Include(r => r.Employee)
                .Include(r => r.Tickets)
                .FirstAsync(r => r.RouteId == route.RouteId);

            LogAuthorizedAction("create route", new { RouteId = route.RouteId, StartPoint = model.StartPoint, EndPoint = model.EndPoint });
            return CreatedAtAction(nameof(GetRoute), new { id = route.RouteId }, route);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageRoutes)] // Requires route management permission
        public async Task<IActionResult> UpdateRoute(long id, [FromBody] UpdateRouteModel model)
        {
            if (model == null)
            {
                return CreateValidationErrorResponse("Update model is required");
            }

            _logger.LogInformation("Updating route with ID {RouteId}", id);
            var route = await _context.Marshuti.FindAsync(id);
            if (route == null)
            {
                return CreateNotFoundResponse("Route", id);
            }

            // Validate bus if provided
            if (model.BusId.HasValue)
            {
                var bus = await _context.Avtobusy.FindAsync(model.BusId.Value);
                if (bus == null)
                {
                    return CreateValidationErrorResponse("Invalid bus ID", new { BusId = model.BusId.Value });
                }
                route.BusId = model.BusId.Value;
            }

            // Validate driver if provided
            if (model.DriverId.HasValue)
            {
                var driver = await _context.Employees.FindAsync(model.DriverId.Value);
                if (driver == null)
                {
                    return CreateValidationErrorResponse("Invalid driver ID", new { DriverId = model.DriverId.Value });
                }
                route.DriverId = model.DriverId.Value;
            }

            // Update other fields if provided
            if (model.StartPoint != null)
            {
                route.StartPoint = model.StartPoint;
            }
            if (model.EndPoint != null)
            {
                route.EndPoint = model.EndPoint;
            }
            if (model.TravelTime != null)
            {
                route.TravelTime = model.TravelTime;
            }

            try
            {
                await _context.SaveChangesAsync();
                LogAuthorizedAction("update route", new { RouteId = id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating route with ID {RouteId}", id);
                if (!await RouteExists(id))
                {
                    return CreateNotFoundResponse("Route", id);
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageRoutes)] // Requires route management permission
        public async Task<IActionResult> DeleteRoute(long id)
        {
            _logger.LogInformation("Deleting route with ID {RouteId}", id);
            var route = await _context.Marshuti.FindAsync(id);
            if (route == null)
            {
                return CreateNotFoundResponse("Route", id);
            }

            _context.Marshuti.Remove(route);
            await _context.SaveChangesAsync();

            LogAuthorizedAction("delete route", new { RouteId = id });
            return NoContent();
        }

        [HttpGet("search")]
        [Authorize(Policy = AuthorizationPolicies.CanViewDashboard)] // Any authenticated user can search routes
        public async Task<ActionResult<IEnumerable<Marshut>>> SearchRoutes(
            [FromQuery] string? startPoint = null,
            [FromQuery] string? endPoint = null,
            [FromQuery] string? busModel = null,
            [FromQuery] string? driverName = null)
        {
            _logger.LogInformation("Searching routes with start point: {StartPoint}, end point: {EndPoint}, bus model: {BusModel}, driver name: {DriverName}",
                startPoint ?? "any", endPoint ?? "any", busModel ?? "any", driverName ?? "any");

            var query = _context.Marshuti
                .Include(r => r.Avtobus)
                .Include(r => r.Employee)
                .Include(r => r.Tickets)
                .AsQueryable();

            if (!string.IsNullOrEmpty(startPoint))
            {
                query = query.Where(r => r.StartPoint.Contains(startPoint));
            }

            if (!string.IsNullOrEmpty(endPoint))
            {
                query = query.Where(r => r.EndPoint.Contains(endPoint));
            }

            if (!string.IsNullOrEmpty(busModel))
            {
                query = query.Where(r => r.Avtobus.Model.Contains(busModel));
            }

            if (!string.IsNullOrEmpty(driverName))
            {
                query = query.Where(r => r.Employee.Name.Contains(driverName) || 
                                       r.Employee.Surname.Contains(driverName));
            }

            var results = await query.ToListAsync();
            _logger.LogDebug("Found {RouteCount} routes matching search criteria", results.Count);
            LogAuthorizedAction("search routes", new { ResultCount = results.Count, StartPoint = startPoint, EndPoint = endPoint, BusModel = busModel, DriverName = driverName });
            return results;
        }

        private async Task<bool> RouteExists(long id)
        {
            _logger.LogDebug("Checking if route with ID {RouteId} exists", id);
            return await _context.Marshuti.AnyAsync(e => e.RouteId == id);
        }
    }

    public class CreateRouteModel
    {
        public required string StartPoint { get; set; }
        public required string EndPoint { get; set; }
        public required long BusId { get; set; }
        public required long DriverId { get; set; }
        public required string TravelTime { get; set; }
    }

    public class UpdateRouteModel
    {
        public string? StartPoint { get; set; }
        public string? EndPoint { get; set; }
        public long? BusId { get; set; }
        public long? DriverId { get; set; }
        public string? TravelTime { get; set; }
    }
}