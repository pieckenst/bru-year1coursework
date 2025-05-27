using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [Authorize] // Allow all authenticated users
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeesController(AppDbContext context)
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
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            Log.Information("Fetching all employees with their job details");
            var employees = await _context.Employees
                .Include(e => e.Job)
                .ToListAsync();
            
            Log.Debug("Retrieved {EmployeeCount} employees", employees.Count);
            return employees;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(long id)
        {
            Log.Information("Fetching employee with ID {EmployeeId}", id);
            var employee = await _context.Employees
                .Include(e => e.Job)
                .FirstOrDefaultAsync(e => e.EmpId == id);

            if (employee == null)
            {
                Log.Warning("Employee with ID {EmployeeId} not found", id);
                return NotFound();
            }

            Log.Debug("Successfully retrieved employee with ID {EmployeeId}", id);
            return employee;
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee(Employee employee)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to create employee by non-admin user");
                return Forbid();
            }

            Log.Information("Creating new employee with name {EmployeeName} {EmployeeSurname}", employee.Name, employee.Surname);

            // Detach the Job object to prevent EF from trying to insert it
            var job = await _context.Jobs.FindAsync(employee.JobId);
            if (job == null)
            {
                Log.Warning("Invalid JobId {JobId} provided for employee creation", employee.JobId);
                return BadRequest("Invalid JobId");
            }
            employee.Job = null;

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Reload the employee with the Job included
            var createdEmployee = await _context.Employees
                .Include(e => e.Job)
                .FirstAsync(e => e.EmpId == employee.EmpId);

            Log.Information("Successfully created employee with ID {EmployeeId}", createdEmployee.EmpId);
            return CreatedAtAction(nameof(GetEmployee), new { id = createdEmployee.EmpId }, createdEmployee);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(long id, Employee employee)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update employee by non-admin user");
                return Forbid();
            }

            if (id != employee.EmpId)
            {
                Log.Warning("ID mismatch in update request. Path ID: {PathId}, Employee ID: {EmployeeId}", id, employee.EmpId);
                return BadRequest();
            }

            Log.Information("Updating employee with ID {EmployeeId}", id);

            // Verify the Job exists
            var job = await _context.Jobs.FindAsync(employee.JobId);
            if (job == null)
            {
                Log.Warning("Invalid JobId {JobId} provided for employee update", employee.JobId);
                return BadRequest("Invalid JobId");
            }

            // Detach the Job object
            employee.Job = null;

            // Attach the employee and mark it as modified
            _context.Entry(employee).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("Successfully updated employee with ID {EmployeeId}", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Log.Error(ex, "Concurrency error updating employee with ID {EmployeeId}", id);
                if (!await EmployeeExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete employee by non-admin user");
                return Forbid();
            }

            Log.Information("Deleting employee with ID {EmployeeId}", id);
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                Log.Warning("Employee with ID {EmployeeId} not found for deletion", id);
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            Log.Information("Successfully deleted employee with ID {EmployeeId}", id);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Employee>>> SearchEmployees(
            [FromQuery] string? surname = null,
            [FromQuery] string? name = null,
            [FromQuery] string? jobTitle = null)
        {
            Log.Information("Searching employees with parameters - Surname: {Surname}, Name: {Name}, JobTitle: {JobTitle}",
                surname ?? "any", name ?? "any", jobTitle ?? "any");

            var query = _context.Employees.Include(e => e.Job).AsQueryable();

            if (!string.IsNullOrEmpty(surname))
                query = query.Where(e => e.Surname.Contains(surname));

            if (!string.IsNullOrEmpty(name))
                query = query.Where(e => e.Name.Contains(name));

            if (!string.IsNullOrEmpty(jobTitle))
                query = query.Where(e => e.Job.JobTitle.Contains(jobTitle));

            var results = await query.ToListAsync();
            Log.Debug("Found {EmployeeCount} employees matching search criteria", results.Count);
            return results;
        }

        private async Task<bool> EmployeeExists(long id)
        {
            return await _context.Employees.AnyAsync(e => e.EmpId == id);
        }
    }
}