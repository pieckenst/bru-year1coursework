using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using Serilog;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
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

        // GET: api/departments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments([FromQuery] bool includeInactive = false)
        {
            Log.Information("Fetching all departments (includeInactive: {IncludeInactive})", includeInactive);
            
            var query = _context.Departments
                .Include(d => d.ParentDepartment)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(d => d.IsActive);
            }

            var departments = await query.ToListAsync();
            
            Log.Debug("Retrieved {DepartmentCount} departments", departments.Count);
            return departments;
        }

        // GET: api/departments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetDepartment(long id)
        {
            Log.Information("Fetching department with ID {DepartmentId}", id);
            
            var department = await _context.Departments
                .Include(d => d.ParentDepartment)
                .Include(d => d.ChildDepartments)
                .Include(d => d.Employees)
                    .ThenInclude(e => e.Job)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);

            if (department == null)
            {
                Log.Warning("Department with ID {DepartmentId} not found", id);
                return NotFound();
            }

            Log.Debug("Successfully retrieved department with ID {DepartmentId}", id);
            return department;
        }

        // GET: api/departments/tree
        [HttpGet("tree")]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartmentTree()
        {
            Log.Information("Fetching department tree structure");
            
            // Get all active departments
            var allDepartments = await _context.Departments
                .Where(d => d.IsActive)
                .Include(d => d.ParentDepartment)
                .Include(d => d.ChildDepartments)
                .ToListAsync();

            // Return only root departments (those without parents)
            var rootDepartments = allDepartments
                .Where(d => d.ParentDepartmentId == null)
                .ToList();

            Log.Debug("Retrieved {RootCount} root departments", rootDepartments.Count);
            return rootDepartments;
        }

        // GET: api/departments/5/employees
        [HttpGet("{id}/employees")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetDepartmentEmployees(long id)
        {
            Log.Information("Fetching employees for department {DepartmentId}", id);
            
            if (!await _context.Departments.AnyAsync(d => d.DepartmentId == id))
                return NotFound($"Department with ID {id} not found");

            var employees = await _context.Employees
                .Where(e => e.DepartmentId == id && e.IsActive)
                .Include(e => e.Job)
                .ToListAsync();

            Log.Debug("Retrieved {EmployeeCount} employees for department {DepartmentId}", employees.Count, id);
            return employees;
        }

        // POST: api/departments
        [HttpPost]
        public async Task<ActionResult<Department>> CreateDepartment(Department department)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to create department by non-admin user");
                return Forbid();
            }

            Log.Information("Creating new department with name {DepartmentName}", department.DepartmentName);

            // Validate parent department if specified
            if (department.ParentDepartmentId.HasValue)
            {
                var parentExists = await _context.Departments.AnyAsync(d => d.DepartmentId == department.ParentDepartmentId.Value);
                if (!parentExists)
                {
                    Log.Warning("Invalid ParentDepartmentId {ParentId} provided", department.ParentDepartmentId.Value);
                    return BadRequest("Invalid ParentDepartmentId");
                }
            }

            // Clear navigation properties to prevent EF from trying to insert them
            department.ParentDepartment = null;
            department.ChildDepartments = null;
            department.Employees = null;

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            Log.Information("Successfully created department with ID {DepartmentId}", department.DepartmentId);
            return CreatedAtAction(nameof(GetDepartment), new { id = department.DepartmentId }, department);
        }

        // PUT: api/departments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(long id, Department department)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update department by non-admin user");
                return Forbid();
            }

            if (id != department.DepartmentId)
            {
                Log.Warning("ID mismatch in update request. Path ID: {PathId}, Department ID: {DepartmentId}", id, department.DepartmentId);
                return BadRequest();
            }

            Log.Information("Updating department with ID {DepartmentId}", id);

            // Validate parent department if specified
            if (department.ParentDepartmentId.HasValue)
            {
                // Prevent circular reference
                if (department.ParentDepartmentId.Value == id)
                {
                    Log.Warning("Circular reference detected: department cannot be its own parent");
                    return BadRequest("Department cannot be its own parent");
                }

                var parentExists = await _context.Departments.AnyAsync(d => d.DepartmentId == department.ParentDepartmentId.Value);
                if (!parentExists)
                {
                    Log.Warning("Invalid ParentDepartmentId {ParentId} provided", department.ParentDepartmentId.Value);
                    return BadRequest("Invalid ParentDepartmentId");
                }
            }

            // Clear navigation properties
            department.ParentDepartment = null;
            department.ChildDepartments = null;
            department.Employees = null;

            _context.Entry(department).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("Successfully updated department with ID {DepartmentId}", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Log.Error(ex, "Concurrency error updating department with ID {DepartmentId}", id);
                if (!await _context.Departments.AnyAsync(d => d.DepartmentId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/departments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete department by non-admin user");
                return Forbid();
            }

            Log.Information("Deleting department with ID {DepartmentId}", id);
            var department = await _context.Departments
                .Include(d => d.Employees)
                .Include(d => d.ChildDepartments)
                .FirstOrDefaultAsync(d => d.DepartmentId == id);
            
            if (department == null)
            {
                Log.Warning("Department with ID {DepartmentId} not found for deletion", id);
                return NotFound();
            }

            // Check if department has employees
            if (department.Employees != null && department.Employees.Any())
            {
                Log.Warning("Cannot delete department {DepartmentId} with {EmployeeCount} employees", id, department.Employees.Count);
                return BadRequest($"Cannot delete department with {department.Employees.Count} employees. Please reassign employees first.");
            }

            // Check if department has child departments
            if (department.ChildDepartments != null && department.ChildDepartments.Any())
            {
                Log.Warning("Cannot delete department {DepartmentId} with {ChildCount} child departments", id, department.ChildDepartments.Count);
                return BadRequest($"Cannot delete department with {department.ChildDepartments.Count} child departments. Please reorganize structure first.");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            Log.Information("Successfully deleted department with ID {DepartmentId}", id);
            return NoContent();
        }

        // PUT: api/departments/5/deactivate
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateDepartment(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to deactivate department by non-admin user");
                return Forbid();
            }

            Log.Information("Deactivating department {DepartmentId}", id);
            var department = await _context.Departments.FindAsync(id);
            
            if (department == null)
                return NotFound();

            department.IsActive = false;
            await _context.SaveChangesAsync();

            Log.Information("Successfully deactivated department {DepartmentId}", id);
            return NoContent();
        }

        // PUT: api/departments/5/activate
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateDepartment(long id)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to activate department by non-admin user");
                return Forbid();
            }

            Log.Information("Activating department {DepartmentId}", id);
            var department = await _context.Departments.FindAsync(id);
            
            if (department == null)
                return NotFound();

            department.IsActive = true;
            await _context.SaveChangesAsync();

            Log.Information("Successfully activated department {DepartmentId}", id);
            return NoContent();
        }
    }
}
