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
    public class BusesController : BaseAuthorizedController
    {
        private readonly AppDbContext _context;

        public BusesController(
            AppDbContext context, 
            ILogger<BusesController> logger,
            IRoleCacheService roleCacheService) 
            : base(logger, roleCacheService)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.CanViewDashboard)] // Any authenticated user can view buses
        public async Task<ActionResult<IEnumerable<Avtobus>>> GetBuses()
        {
            _logger.LogInformation("Fetching all buses with their routes and service records");
            var buses = await _context.Avtobusy
                .Include(b => b.Routes)
                .Include(b => b.Obsluzhivanies)
                .ToListAsync();
            
            _logger.LogDebug("Retrieved {BusCount} buses", buses.Count);
            LogAuthorizedAction("view buses", new { Count = buses.Count });
            return buses;
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.CanViewDashboard)] // Any authenticated user can view a specific bus
        public async Task<ActionResult<Avtobus>> GetBus(long id)
        {
            _logger.LogInformation("Fetching bus with ID {BusId}", id);
            var bus = await _context.Avtobusy
                .Include(b => b.Routes)
                .Include(b => b.Obsluzhivanies)
                .FirstOrDefaultAsync(b => b.BusId == id);

            if (bus == null)
            {
                _logger.LogWarning("Bus with ID {BusId} not found", id);
                return NotFound(new { Message = "Bus not found", Id = id });
            }

            _logger.LogDebug("Successfully retrieved bus with ID {BusId}", id);
            LogAuthorizedAction("view bus", new { BusId = id });
            return bus;
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.CanManageBuses)] // Requires bus management permission
        public async Task<ActionResult<Avtobus>> CreateBus([FromBody] CreateBusModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Model))
            {
                _logger.LogWarning("Invalid bus model provided");
                return BadRequest(new { Message = "Bus model is required" });
            }

            _logger.LogInformation("Creating new bus with model {Model}", model.Model);
            var bus = new Avtobus
            {
                Model = model.Model
            };

            _context.Avtobusy.Add(bus);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            bus = await _context.Avtobusy
                .Include(b => b.Routes)
                .Include(b => b.Obsluzhivanies)
                .FirstAsync(b => b.BusId == bus.BusId);

            LogAuthorizedAction("create bus", new { BusId = bus.BusId, Model = model.Model });
            return CreatedAtAction(nameof(GetBus), new { id = bus.BusId }, bus);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageBuses)] // Requires bus management permission
        public async Task<IActionResult> UpdateBus(long id, [FromBody] UpdateBusModel model)
        {
            if (model == null)
            {
                return CreateValidationErrorResponse("Update model is required");
            }

            _logger.LogInformation("Updating bus with ID {BusId}", id);
            var bus = await _context.Avtobusy.FindAsync(id);
            if (bus == null)
            {
                return CreateNotFoundResponse("Bus", id);
            }

            if (model.Model != null)
            {
                _logger.LogDebug("Updating bus model from {OldModel} to {NewModel}", bus.Model, model.Model);
                bus.Model = model.Model;
            }

            try
            {
                await _context.SaveChangesAsync();
                LogAuthorizedAction("update bus", new { BusId = id, NewModel = model.Model });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating bus with ID {BusId}", id);
                if (!await BusExists(id))
                {
                    return CreateNotFoundResponse("Bus", id);
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.CanManageBuses)] // Requires bus management permission
        public async Task<IActionResult> DeleteBus(long id)
        {
            _logger.LogInformation("Deleting bus with ID {BusId}", id);
            var bus = await _context.Avtobusy.FindAsync(id);
            if (bus == null)
            {
                return CreateNotFoundResponse("Bus", id);
            }

            _context.Avtobusy.Remove(bus);
            await _context.SaveChangesAsync();

            LogAuthorizedAction("delete bus", new { BusId = id });
            return NoContent();
        }

        [HttpGet("search")]
        [Authorize(Policy = AuthorizationPolicies.CanViewDashboard)] // Any authenticated user can search buses
        public async Task<ActionResult<IEnumerable<Avtobus>>> SearchBuses(
            [FromQuery] string? model = null,
            [FromQuery] string? serviceStatus = null)
        {
            _logger.LogInformation("Searching buses with model: {Model}, service status: {ServiceStatus}", 
                model ?? "any", serviceStatus ?? "any");

            var query = _context.Avtobusy
                .Include(b => b.Routes)
                .Include(b => b.Obsluzhivanies)
                .AsQueryable();

            if (!string.IsNullOrEmpty(model))
            {
                _logger.LogDebug("Filtering by model containing: {Model}", model);
                query = query.Where(b => b.Model.Contains(model));
            }

            if (!string.IsNullOrEmpty(serviceStatus))
            {
                _logger.LogDebug("Filtering by service status: {ServiceStatus}", serviceStatus);
                query = query.Where(b => b.Obsluzhivanies.Any(m => m.Roadworthiness == serviceStatus));
            }

            var results = await query.ToListAsync();
            _logger.LogDebug("Found {ResultCount} buses matching search criteria", results.Count);
            LogAuthorizedAction("search buses", new { ResultCount = results.Count, Model = model, ServiceStatus = serviceStatus });
            return results;
        }

        private async Task<bool> BusExists(long id)
        {
            _logger.LogDebug("Checking if bus with ID {BusId} exists", id);
            return await _context.Avtobusy.AnyAsync(e => e.BusId == id);
        }
    }

    public class CreateBusModel
    {
        public required string Model { get; set; }
    }

    public class UpdateBusModel
    {
        public string? Model { get; set; }
    }
} 