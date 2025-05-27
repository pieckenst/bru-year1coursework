
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TicketSalesApp.Core.Data;
    using TicketSalesApp.Core.Models;
    using TicketSalesApp.Services.Interfaces;
    using System.IdentityModel.Tokens.Jwt;
    using Serilog;
    using Microsoft.IdentityModel.Tokens;
    using System.Security.Claims;
    using System.Text;

    namespace TicketSalesApp.AdminServer.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        [Authorize] // Allow all authenticated users to read
        public class TicketSalesController : ControllerBase
        {
            private readonly AppDbContext _context;
            private readonly ITicketSalesService _ticketSalesService;
            private readonly IConfiguration _configuration;

            public TicketSalesController(AppDbContext context, ITicketSalesService ticketSalesService, IConfiguration configuration)
            {
                _context = context;
                _ticketSalesService = ticketSalesService;
                _configuration = configuration;
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
            public async Task<ActionResult<IEnumerable<Prodazha>>> GetTicketSales()
            {
                Log.Information("Fetching all ticket sales with related data");
                var sales = await _context.Prodazhi
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Employee)
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Avtobus)
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();
            
                Log.Debug("Retrieved {SalesCount} ticket sales with full route information", sales.Count);
                return sales;
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<Prodazha>> GetTicketSale(long id)
            {
                Log.Information("Fetching ticket sale with ID {SaleId} and related data", id);
                var sale = await _context.Prodazhi
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Employee)
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Avtobus)
                    .FirstOrDefaultAsync(s => s.SaleId == id);

                if (sale == null)
                {
                    Log.Warning("Ticket sale with ID {SaleId} not found", id);
                    return NotFound();
                }

                Log.Debug("Successfully retrieved ticket sale with ID {SaleId} and full route information", id);
                return sale;
            }

            [HttpPost]
            public async Task<ActionResult<Prodazha>> CreateTicketSale([FromBody] CreateTicketSaleModel model)
            {
                if (!IsAdmin())
                {
                    Log.Warning("Unauthorized attempt to create ticket sale by non-admin user");
                    return Forbid();
                }

                Log.Information("Creating new ticket sale for ticket ID {TicketId}", model.TicketId);
                var ticket = await _context.Bilety
                    .Include(t => t.Sales)
                    .Include(t => t.Marshut)
                        .ThenInclude(m => m.Employee)
                    .Include(t => t.Marshut)
                        .ThenInclude(m => m.Avtobus)
                    .FirstOrDefaultAsync(t => t.TicketId == model.TicketId);

                if (ticket == null)
                {
                    Log.Warning("Invalid ticket ID {TicketId} provided for sale creation", model.TicketId);
                    return BadRequest("Invalid ticket ID");
                }

                if (ticket.Sales.Any())
                {
                    Log.Warning("Ticket with ID {TicketId} is already sold", model.TicketId);
                    return BadRequest("Ticket is already sold");
                }

                // Get current user info using proper token validation
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    Log.Warning("Missing or invalid Authorization header");
                    return Unauthorized(new { message = "Missing or invalid Authorization header" });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var tokenHandler = new JwtSecurityTokenHandler();

                if (!tokenHandler.CanReadToken(token))
                {
                    Log.Warning("Invalid JWT token format");
                    return Unauthorized(new { message = "Invalid token format" });
                }

                // Get the JWT secret key
                var keyString = _configuration["JwtSettings:Secret"] ?? 
                    throw new InvalidOperationException("JWT secret is not configured");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

                // Set up token validation parameters
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                // Validate the token
                ClaimsPrincipal principal;
                try
                {
                    principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                    Log.Debug("Token validation successful");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Token validation failed");
                    return Unauthorized(new { message = "Invalid token", error = ex.Message });
                }

                // Get username from validated claims
                var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name) ?? 
                                   principal.Claims.FirstOrDefault(c => c.Type == "name") ??
                                   principal.Claims.FirstOrDefault(c => c.Type == "sub");

                if (usernameClaim == null)
                {
                    Log.Warning("No username claim found in validated token");
                    return Unauthorized(new { message = "Invalid token: no username claim found" });
                }

                // Get current user with phone number
                var currentUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Login == usernameClaim.Value);

                if (currentUser == null)
                {
                    Log.Warning("User from token not found in database: {Username}", usernameClaim.Value);
                    return NotFound(new { message = $"User '{usernameClaim.Value}' not found" });
                }

                var sale = new Prodazha
                {
                    TicketId = model.TicketId,
                    SaleDate = model.SaleDate,
                    TicketSoldToUser = model.TicketSoldToUser ?? currentUser.Login,
                    TicketSoldToUserPhone = model.TicketSoldToUserPhone ?? currentUser.PhoneNumber ?? "+375333000000"
                };

                _context.Prodazhi.Add(sale);
                await _context.SaveChangesAsync();

                // Reload with navigation properties
                sale = await _context.Prodazhi
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Employee)
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Avtobus)
                    .FirstAsync(s => s.SaleId == sale.SaleId);

                Log.Information("Successfully created ticket sale with ID {SaleId} for user {User} with phone {Phone}", 
                    sale.SaleId, sale.TicketSoldToUser, sale.TicketSoldToUserPhone);
                return CreatedAtAction(nameof(GetTicketSale), new { id = sale.SaleId }, sale);
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateTicketSale(long id, [FromBody] UpdateTicketSaleModel model)
            {
                if (!IsAdmin())
                {
                    Log.Warning("Unauthorized attempt to update ticket sale by non-admin user");
                    return Forbid();
                }

                Log.Information("Updating ticket sale with ID {SaleId}", id);
                var sale = await _context.Prodazhi
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                    .FirstOrDefaultAsync(s => s.SaleId == id);

                if (sale == null)
                {
                    Log.Warning("Ticket sale with ID {SaleId} not found for update", id);
                    return NotFound();
                }

                if (model.TicketId.HasValue)
                {
                    var ticket = await _context.Bilety
                        .Include(t => t.Marshut)
                            .ThenInclude(m => m.Employee)
                        .Include(t => t.Marshut)
                            .ThenInclude(m => m.Avtobus)
                        .FirstOrDefaultAsync(t => t.TicketId == model.TicketId.Value);

                    if (ticket == null)
                    {
                        Log.Warning("Invalid ticket ID {TicketId} provided for sale update", model.TicketId.Value);
                        return BadRequest("Invalid ticket ID");
                    }
                    sale.TicketId = model.TicketId.Value;
                    sale.Bilet = ticket;
                }

                if (model.SaleDate.HasValue)
                    sale.SaleDate = model.SaleDate.Value;

                if (model.TicketSoldToUser != null)
                    sale.TicketSoldToUser = model.TicketSoldToUser;

                if (model.TicketSoldToUserPhone != null)
                    sale.TicketSoldToUserPhone = model.TicketSoldToUserPhone;

                try
                {
                    await _context.SaveChangesAsync();
                    Log.Information("Successfully updated ticket sale with ID {SaleId}", id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await TicketSaleExists(id))
                    {
                        Log.Warning("Ticket sale with ID {SaleId} not found during concurrency update", id);
                        return NotFound();
                    }
                    throw;
                }

                return NoContent();
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteTicketSale(long id)
            {
                if (!IsAdmin())
                {
                    Log.Warning("Unauthorized attempt to delete ticket sale by non-admin user");
                    return Forbid();
                }

                Log.Information("Deleting ticket sale with ID {SaleId}", id);
                var sale = await _context.Prodazhi.FindAsync(id);
                if (sale == null)
                {
                    Log.Warning("Ticket sale with ID {SaleId} not found for deletion", id);
                    return NotFound();
                }

                _context.Prodazhi.Remove(sale);
                await _context.SaveChangesAsync();

                Log.Information("Successfully deleted ticket sale with ID {SaleId}", id);
                return NoContent();
            }

            [HttpGet("statistics/income")]
            public async Task<ActionResult<decimal>> GetTotalIncome(int year, int month)
            {
                Log.Information("Fetching total income for {Year}-{Month}", year, month);
                var income = await _ticketSalesService.GetTotalIncomeAsync(year, month);
                Log.Debug("Total income for {Year}-{Month}: {Income}", year, month, income);
                return income;
            }

            [HttpGet("statistics/top-transports")]
            public async Task<ActionResult<List<TransportStatistic>>> GetTopTransports(int year, int month)
            {
                Log.Information("Fetching top transports for {Year}-{Month}", year, month);
                var transports = await _ticketSalesService.GetTopTransportsAsync(year, month);
                Log.Debug("Found {TransportCount} top transports for {Year}-{Month}", transports.Count, year, month);
                return transports;
            }

            [HttpGet("search")]
            public async Task<ActionResult<IEnumerable<Prodazha>>> SearchSales(
                [FromQuery] DateTime? startDate = null,
                [FromQuery] DateTime? endDate = null,
                [FromQuery] decimal? minPrice = null,
                [FromQuery] decimal? maxPrice = null,
                [FromQuery] string? soldToUser = null)
            {
                Log.Information("Searching sales with start date: {StartDate}, end date: {EndDate}, min price: {MinPrice}, max price: {MaxPrice}, user: {User}",
                    startDate?.ToString() ?? "any", endDate?.ToString() ?? "any", minPrice?.ToString() ?? "any", maxPrice?.ToString() ?? "any", soldToUser ?? "any");

                var query = _context.Prodazhi
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Employee)
                    .Include(s => s.Bilet)
                        .ThenInclude(t => t.Marshut)
                            .ThenInclude(m => m.Avtobus)
                    .AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(s => s.SaleDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(s => s.SaleDate <= endDate.Value);

                if (minPrice.HasValue)
                    query = query.Where(s => s.Bilet.TicketPrice >= minPrice.Value);

                if (maxPrice.HasValue)
                    query = query.Where(s => s.Bilet.TicketPrice <= maxPrice.Value);

                if (!string.IsNullOrEmpty(soldToUser))
                    query = query.Where(s => s.TicketSoldToUser == soldToUser);

                var results = await query.OrderByDescending(s => s.SaleDate).ToListAsync();
                Log.Debug("Found {SalesCount} sales matching search criteria", results.Count);
                return results;
            }

            private async Task<bool> TicketSaleExists(long id)
            {
                return await _context.Prodazhi.AnyAsync(e => e.SaleId == id);
            }
        }

        public class CreateTicketSaleModel
        {
            public required long TicketId { get; set; }
            public required DateTime SaleDate { get; set; }
            public string? TicketSoldToUser { get; set; } = "ФИЗ.ПРОДАЖА";
            public string? TicketSoldToUserPhone { get; set; }
        }

        public class UpdateTicketSaleModel
        {
            public long? TicketId { get; set; }
            public DateTime? SaleDate { get; set; }
            public string? TicketSoldToUser { get; set; } = "ФИЗ.ПРОДАЖА";
            public string? TicketSoldToUserPhone { get; set; }
        }
    } 