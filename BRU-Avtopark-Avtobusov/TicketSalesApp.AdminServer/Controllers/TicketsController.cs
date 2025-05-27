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
    using Microsoft.Extensions.Logging;

    namespace TicketSalesApp.AdminServer.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        [Authorize] // Allow all authenticated users to read
        public class TicketsController : ControllerBase
        {
            private readonly AppDbContext _context;
            private readonly ILogger<TicketsController> _logger;

            public TicketsController(AppDbContext context, ILogger<TicketsController> logger)
            {
                _context = context;
                _logger = logger;
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
            public async Task<ActionResult<IEnumerable<Bilet>>> GetTickets()
            {
                _logger.LogInformation("Fetching all tickets with route information");
                var tickets = await _context.Bilety
                    .Include(t => t.Marshut)
                    .Include(t => t.Sales)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} tickets", tickets.Count);
                return tickets;
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<Bilet>> GetTicket(long id)
            {
                _logger.LogInformation("Fetching ticket {TicketId}", id);
                var ticket = await _context.Bilety
                    .Include(t => t.Marshut)
                    .Include(t => t.Sales)
                    .FirstOrDefaultAsync(t => t.TicketId == id);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket {TicketId} not found", id);
                    return NotFound();
                }

                _logger.LogDebug("Successfully retrieved ticket {TicketId}", id);
                return ticket;
            }

            [HttpPost]
            public async Task<ActionResult<Bilet>> CreateTicket([FromBody] CreateTicketModel model)
            {
                if (!IsAdmin()) return Forbid();

                var route = await _context.Marshuti.FindAsync(model.RouteId);
                if (route == null)
                {
                    return BadRequest("Invalid route ID");
                }

                var ticket = new Bilet
                {
                    RouteId = model.RouteId,
                    TicketPrice = model.TicketPrice
                };

                _context.Bilety.Add(ticket);
                await _context.SaveChangesAsync();

                // Reload with navigation properties
                ticket = await _context.Bilety
                    .Include(t => t.Marshut)
                    .Include(t => t.Sales)
                    .FirstAsync(t => t.TicketId == ticket.TicketId);

                return CreatedAtAction(nameof(GetTicket), new { id = ticket.TicketId }, ticket);
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateTicket(long id, [FromBody] UpdateTicketModel model)
            {
                if (!IsAdmin()) return Forbid();

                var ticket = await _context.Bilety.FindAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                if (model.RouteId.HasValue)
                {
                    var route = await _context.Marshuti.FindAsync(model.RouteId.Value);
                    if (route == null)
                    {
                        return BadRequest("Invalid route ID");
                    }
                    ticket.RouteId = model.RouteId.Value;
                }

                if (model.TicketPrice.HasValue)
                    ticket.TicketPrice = model.TicketPrice.Value;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await TicketExists(id))
                    {
                        return NotFound();
                    }
                    throw;
                }

                return NoContent();
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteTicket(long id)
            {
                if (!IsAdmin()) return Forbid();

                var ticket = await _context.Bilety.FindAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                _context.Bilety.Remove(ticket);
                await _context.SaveChangesAsync();

                return NoContent();
            }

            [HttpGet("search")]
            public async Task<ActionResult<IEnumerable<Bilet>>> SearchTickets(
                [FromQuery] long? routeId = null,
                [FromQuery] decimal? minPrice = null,
                [FromQuery] decimal? maxPrice = null,
                [FromQuery] bool? isSold = null)
            {
                _logger.LogInformation("Searching tickets with routeId: {RouteId}, minPrice: {MinPrice}, maxPrice: {MaxPrice}, isSold: {IsSold}",
                    routeId?.ToString() ?? "any", minPrice?.ToString() ?? "any", maxPrice?.ToString() ?? "any", isSold?.ToString() ?? "any");

                var query = _context.Bilety
                    .Include(t => t.Marshut)
                    .Include(t => t.Sales)
                    .AsQueryable();

                if (routeId.HasValue)
                    query = query.Where(t => t.RouteId == routeId.Value);

                if (minPrice.HasValue)
                    query = query.Where(t => t.TicketPrice >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(t => t.TicketPrice <= maxPrice.Value);

                if (isSold.HasValue)
                {
                    if (isSold.Value)
                        query = query.Where(t => t.Sales.Any());
                    else
                        query = query.Where(t => !t.Sales.Any());
                }

                return await query.ToListAsync();
            }

            [HttpGet("route/{routeId}")]
            public async Task<ActionResult<IEnumerable<Bilet>>> GetTicketsByRoute(long routeId)
            {
                _logger.LogInformation("Fetching tickets for route {RouteId}", routeId);

                // First verify route exists
                var route = await _context.Marshuti.FindAsync(routeId);
                if (route == null)
                {
                    _logger.LogWarning("Route {RouteId} not found", routeId);
                    return NotFound($"Route {routeId} not found");
                }

                var tickets = await _context.Bilety
                    .Include(t => t.Marshut)
                    .Include(t => t.Sales)
                    .Where(t => t.RouteId == routeId)
                    .ToListAsync();

                _logger.LogDebug("Found {Count} tickets for route {RouteId}", tickets.Count, routeId);
                return tickets;
            }

            [HttpGet("available")]
            public async Task<ActionResult<IEnumerable<Bilet>>> GetAvailableTickets()
            {
                _logger.LogInformation("Fetching available tickets with route information");
                var tickets = await _context.Bilety
                    .Include(t => t.Marshut)
                    .Include(t => t.Sales)
                    .Where(t => !t.Sales.Any())
                    .ToListAsync();

                _logger.LogDebug("Found {Count} available tickets", tickets.Count);
                return tickets;
            }

            private async Task<bool> TicketExists(long id)
            {
                return await _context.Bilety.AnyAsync(e => e.TicketId == id);
            }
        }

        public class CreateTicketModel
        {
            public required long RouteId { get; set; }
            public required decimal TicketPrice { get; set; }
        }

        public class UpdateTicketModel
        {
            public long? RouteId { get; set; }
            public decimal? TicketPrice { get; set; }
        }
    } 