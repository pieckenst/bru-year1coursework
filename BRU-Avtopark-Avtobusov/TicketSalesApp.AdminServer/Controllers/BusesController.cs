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

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Allow all authenticated users to read
    public class BusesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BusesController(AppDbContext context)
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Avtobus>>> GetBuses()
        {
            Log.Information("Fetching all buses with their routes and service records");
            var buses = await _context.Avtobusy
                .Include(b => b.Routes)
                .Include(b => b.Obsluzhivanies)
                .ToListAsync();
            
            Log.Debug("Retrieved {BusCount} buses", buses.Count);
            return buses;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Avtobus>> GetBus(long id)
        {
            Log.Information("Fetching bus with ID {BusId}", id);
            var bus = await _context.Avtobusy
                .Include(b => b.Routes)
                .Include(b => b.Obsluzhivanies)
                .FirstOrDefaultAsync(b => b.BusId == id);

            if (bus == null)
            {
                Log.Warning("Bus with ID {BusId} not found", id);
                return NotFound();
            }

            Log.Debug("Successfully retrieved bus with ID {BusId}", id);
            return bus;
        }

        [HttpPost]
        public async Task<ActionResult<Avtobus>> CreateBus([FromBody] CreateBusModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to create bus by non-admin user");
                return Forbid();
            }

            Log.Information("Creating new bus with model {Model}", model.Model);
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

            Log.Information("Successfully created bus with ID {BusId}", bus.BusId);
            return CreatedAtAction(nameof(GetBus), new { id = bus.BusId }, bus);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBus(long id, [FromBody] UpdateBusModel model)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update bus by non-admin user");
                return Forbid();
            }

            Log.Information("Updating bus with ID {BusId}", id);
            var bus = await _context.Avtobusy.FindAsync(id);
            if (bus == null)
            {
                Log.Warning("Bus with ID {BusId} not found for update", id);
                return NotFound();
            }

            if (model.Model != null)
            {
                Log.Debug("Updating bus model from {OldModel} to {NewModel}", bus.Model, model.Model);
                bus.Model = model.Model;
            }

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("Successfully updated bus with ID {BusId}", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Log.Error(ex, "Concurrency error updating bus with ID {BusId}", id);
                if (!await BusExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBus(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete bus by non-admin user");
                return Forbid();
            }

            Log.Information("Deleting bus with ID {BusId}", id);
            var bus = await _context.Avtobusy.FindAsync(id);
            if (bus == null)
            {
                Log.Warning("Bus with ID {BusId} not found for deletion", id);
                return NotFound();
            }

            _context.Avtobusy.Remove(bus);
            await _context.SaveChangesAsync();

            Log.Information("Successfully deleted bus with ID {BusId}", id);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Avtobus>>> SearchBuses(
            [FromQuery] string? model = null,
            [FromQuery] string? serviceStatus = null)
        {
            Log.Information("Searching buses with model: {Model}, service status: {ServiceStatus}", 
                model ?? "any", serviceStatus ?? "any");

            var query = _context.Avtobusy
                .Include(b => b.Routes)
                .Include(b => b.Obsluzhivanies)
                .AsQueryable();

            if (!string.IsNullOrEmpty(model))
            {
                Log.Debug("Filtering by model containing: {Model}", model);
                query = query.Where(b => b.Model.Contains(model));
            }

            if (!string.IsNullOrEmpty(serviceStatus))
            {
                Log.Debug("Filtering by service status: {ServiceStatus}", serviceStatus);
                query = query.Where(b => b.Obsluzhivanies.Any(m => m.Roadworthiness == serviceStatus));
            }

            var results = await query.ToListAsync();
            Log.Debug("Found {ResultCount} buses matching search criteria", results.Count);
            return results;
        }

        private async Task<bool> BusExists(long id)
        {
            Log.Debug("Checking if bus with ID {BusId} exists", id);
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