using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FormsController> _logger;

        public FormsController(AppDbContext context, ILogger<FormsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/forms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FormDefinitionDto>>> GetForms()
        {
            try
            {
                var forms = await _context.FormDefinitions
                    .Where(f => f.IsActive)
                    .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                    .Select(f => MapToDto(f))
                    .ToListAsync();

                return Ok(forms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forms");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving forms");
            }
        }

        // GET: api/forms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<FormDefinitionDto>> GetForm(Guid id)
        {
            try
            {
                var form = await _context.FormDefinitions.FindAsync(id);
                if (form == null || !form.IsActive)
                {
                    return NotFound();
                }

                return Ok(MapToDto(form));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving form with ID: {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving form");
            }
        }

        // POST: api/forms
        [HttpPost]
        public async Task<ActionResult<FormDefinitionDto>> CreateForm([FromBody] CreateFormDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var form = new FormDefinition
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    JsonSchema = createDto.JsonSchema,
                    CreatedBy = userId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.FormDefinitions.Add(form);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetForm), new { id = form.Id }, MapToDto(form));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating form");
            }
        }

        // PUT: api/forms/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateForm(Guid id, [FromBody] UpdateFormDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var form = await _context.FormDefinitions.FindAsync(id);
                if (form == null || !form.IsActive)
                {
                    return NotFound();
                }

                // Update fields
                form.Name = updateDto.Name ?? form.Name;
                form.Description = updateDto.Description;
                form.JsonSchema = updateDto.JsonSchema ?? form.JsonSchema;
                form.UpdatedAt = DateTime.UtcNow;
                form.IsActive = updateDto.IsActive ?? form.IsActive;

                await _context.SaveChangesAsync();
                return Ok(MapToDto(form));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating form with ID: {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating form");
            }
        }

        // DELETE: api/forms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForm(Guid id)
        {
            try
            {
                var form = await _context.FormDefinitions.FindAsync(id);
                if (form == null || !form.IsActive)
                {
                    return NotFound();
                }

                // Soft delete
                form.IsActive = false;
                form.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting form with ID: {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting form");
            }
        }

        // DTOs for request/response
        public class FormDefinitionDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string JsonSchema { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string CreatedBy { get; set; }
            public bool IsActive { get; set; }
        }

        public class CreateFormDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string JsonSchema { get; set; }
        }

        public class UpdateFormDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string JsonSchema { get; set; }
            public bool? IsActive { get; set; }
        }

        private static FormDefinitionDto MapToDto(FormDefinition form)
        {
            if (form == null) return null;

            return new FormDefinitionDto
            {
                Id = form.Id,
                Name = form.Name,
                Description = form.Description,
                JsonSchema = form.JsonSchema,
                CreatedAt = form.CreatedAt,
                UpdatedAt = form.UpdatedAt,
                CreatedBy = form.CreatedBy,
                IsActive = form.IsActive
            };
        }
    }
}
