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
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees([FromQuery] bool includeInactive = false)
        {
            Log.Information("Fetching all employees with their job details (includeInactive: {IncludeInactive})", includeInactive);
            var query = _context.Employees
                .Include(e => e.Job)
                .Include(e => e.Department)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            var employees = await query.ToListAsync();
            
            Log.Debug("Retrieved {EmployeeCount} employees", employees.Count);
            return employees;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(long id, [FromQuery] bool includeDetails = false)
        {
            Log.Information("Fetching employee with ID {EmployeeId} (includeDetails: {IncludeDetails})", id, includeDetails);
            var query = _context.Employees
                .Include(e => e.Job)
                .Include(e => e.Department)
                .AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(e => e.Documents)
                    .Include(e => e.Trainings)
                    .Include(e => e.EmergencyContacts)
                    .Include(e => e.VacationRequests);
            }

            var employee = await query.FirstOrDefaultAsync(e => e.EmpId == id);

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

            if (employee.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(employee.DepartmentId.Value);
                if (department == null)
                {
                    Log.Warning("Invalid DepartmentId {DepartmentId} provided for employee creation", employee.DepartmentId);
                    return BadRequest("Invalid DepartmentId");
                }

                employee.Department = null;
            }

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

            if (employee.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(employee.DepartmentId.Value);
                if (department == null)
                {
                    Log.Warning("Invalid DepartmentId {DepartmentId} provided for employee update", employee.DepartmentId);
                    return BadRequest("Invalid DepartmentId");
                }

                employee.Department = null;
            }

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

        // === Employee Documents Management ===

        [HttpGet("{id}/documents")]
        public async Task<ActionResult<IEnumerable<EmployeeDocument>>> GetEmployeeDocuments(long id)
        {
            Log.Information("Fetching documents for employee {EmployeeId}", id);
            
            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            var documents = await _context.EmployeeDocuments
                .Where(d => d.EmployeeId == id)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            Log.Debug("Retrieved {DocumentCount} documents for employee {EmployeeId}", documents.Count, id);
            return documents;
        }

        [HttpGet("documents/{documentId}")]
        public async Task<ActionResult<EmployeeDocument>> GetDocument(long documentId)
        {
            Log.Information("Fetching document {DocumentId}", documentId);
            var document = await _context.EmployeeDocuments.FindAsync(documentId);

            if (document == null)
            {
                Log.Warning("Document {DocumentId} not found", documentId);
                return NotFound();
            }

            return document;
        }

        [HttpPost("{id}/documents")]
        public async Task<ActionResult<EmployeeDocument>> AddDocument(long id, EmployeeDocument document)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to add document by non-admin user");
                return Forbid();
            }

            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            Log.Information("Adding document for employee {EmployeeId}", id);
            
            document.EmployeeId = id;
            document.CreatedAt = DateTime.UtcNow;
            
            _context.EmployeeDocuments.Add(document);
            await _context.SaveChangesAsync();

            Log.Information("Successfully added document {DocumentId} for employee {EmployeeId}", document.DocumentId, id);
            return CreatedAtAction(nameof(GetDocument), new { documentId = document.DocumentId }, document);
        }

        [HttpPut("documents/{documentId}")]
        public async Task<IActionResult> UpdateDocument(long documentId, EmployeeDocument document)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to update document by non-admin user");
                return Forbid();
            }

            if (documentId != document.DocumentId)
                return BadRequest();

            Log.Information("Updating document {DocumentId}", documentId);
            
            document.UpdatedAt = DateTime.UtcNow;
            _context.Entry(document).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                Log.Information("Successfully updated document {DocumentId}", documentId);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.EmployeeDocuments.AnyAsync(d => d.DocumentId == documentId))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("documents/{documentId}")]
        public async Task<IActionResult> DeleteDocument(long documentId)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete document by non-admin user");
                return Forbid();
            }

            Log.Information("Deleting document {DocumentId}", documentId);
            var document = await _context.EmployeeDocuments.FindAsync(documentId);
            
            if (document == null)
                return NotFound();

            _context.EmployeeDocuments.Remove(document);
            await _context.SaveChangesAsync();

            Log.Information("Successfully deleted document {DocumentId}", documentId);
            return NoContent();
        }

        // === Employee Training Management ===

        [HttpGet("{id}/trainings")]
        public async Task<ActionResult<IEnumerable<EmployeeTraining>>> GetEmployeeTrainings(long id)
        {
            Log.Information("Fetching trainings for employee {EmployeeId}", id);
            
            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            var trainings = await _context.EmployeeTrainings
                .Where(t => t.EmployeeId == id)
                .OrderByDescending(t => t.CompletionDate)
                .ToListAsync();

            Log.Debug("Retrieved {TrainingCount} trainings for employee {EmployeeId}", trainings.Count, id);
            return trainings;
        }

        [HttpPost("{id}/trainings")]
        public async Task<ActionResult<EmployeeTraining>> AddTraining(long id, EmployeeTraining training)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to add training by non-admin user");
                return Forbid();
            }

            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            Log.Information("Adding training for employee {EmployeeId}", id);
            
            training.EmployeeId = id;
            training.CreatedAt = DateTime.UtcNow;
            
            _context.EmployeeTrainings.Add(training);
            await _context.SaveChangesAsync();

            Log.Information("Successfully added training {TrainingId} for employee {EmployeeId}", training.TrainingId, id);
            return CreatedAtAction(nameof(GetEmployeeTrainings), new { id }, training);
        }

        [HttpDelete("trainings/{trainingId}")]
        public async Task<IActionResult> DeleteTraining(long trainingId)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete training by non-admin user");
                return Forbid();
            }

            Log.Information("Deleting training {TrainingId}", trainingId);
            var training = await _context.EmployeeTrainings.FindAsync(trainingId);
            
            if (training == null)
                return NotFound();

            _context.EmployeeTrainings.Remove(training);
            await _context.SaveChangesAsync();

            Log.Information("Successfully deleted training {TrainingId}", trainingId);
            return NoContent();
        }

        // === Emergency Contacts Management ===

        [HttpGet("{id}/emergency-contacts")]
        public async Task<ActionResult<IEnumerable<EmergencyContact>>> GetEmergencyContacts(long id)
        {
            Log.Information("Fetching emergency contacts for employee {EmployeeId}", id);
            
            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            var contacts = await _context.EmergencyContacts
                .Where(c => c.EmployeeId == id)
                .OrderByDescending(c => c.IsPrimary)
                .ToListAsync();

            Log.Debug("Retrieved {ContactCount} emergency contacts for employee {EmployeeId}", contacts.Count, id);
            return contacts;
        }

        [HttpPost("{id}/emergency-contacts")]
        public async Task<ActionResult<EmergencyContact>> AddEmergencyContact(long id, EmergencyContact contact)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to add emergency contact by non-admin user");
                return Forbid();
            }

            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            Log.Information("Adding emergency contact for employee {EmployeeId}", id);
            
            contact.EmployeeId = id;
            
            _context.EmergencyContacts.Add(contact);
            await _context.SaveChangesAsync();

            Log.Information("Successfully added emergency contact {ContactId} for employee {EmployeeId}", contact.ContactId, id);
            return CreatedAtAction(nameof(GetEmergencyContacts), new { id }, contact);
        }

        [HttpDelete("emergency-contacts/{contactId}")]
        public async Task<IActionResult> DeleteEmergencyContact(long contactId)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to delete emergency contact by non-admin user");
                return Forbid();
            }

            Log.Information("Deleting emergency contact {ContactId}", contactId);
            var contact = await _context.EmergencyContacts.FindAsync(contactId);
            
            if (contact == null)
                return NotFound();

            _context.EmergencyContacts.Remove(contact);
            await _context.SaveChangesAsync();

            Log.Information("Successfully deleted emergency contact {ContactId}", contactId);
            return NoContent();
        }

        // === Vacation Requests Management ===

        [HttpGet("{id}/vacation-requests")]
        public async Task<ActionResult<IEnumerable<VacationRequest>>> GetVacationRequests(long id)
        {
            Log.Information("Fetching vacation requests for employee {EmployeeId}", id);
            
            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            var requests = await _context.VacationRequests
                .Where(v => v.EmployeeId == id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            Log.Debug("Retrieved {RequestCount} vacation requests for employee {EmployeeId}", requests.Count, id);
            return requests;
        }

        [HttpPost("{id}/vacation-requests")]
        public async Task<ActionResult<VacationRequest>> CreateVacationRequest(long id, VacationRequest request)
        {
            if (!await EmployeeExists(id))
                return NotFound($"Employee with ID {id} not found");

            Log.Information("Creating vacation request for employee {EmployeeId}", id);
            
            request.EmployeeId = id;
            request.Status = "Pending";
            request.CreatedAt = DateTime.UtcNow;
            
            // Calculate days requested
            request.DaysRequested = (int)(request.EndDate - request.StartDate).TotalDays + 1;
            
            _context.VacationRequests.Add(request);
            await _context.SaveChangesAsync();

            Log.Information("Successfully created vacation request {RequestId} for employee {EmployeeId}", request.RequestId, id);
            return CreatedAtAction(nameof(GetVacationRequests), new { id }, request);
        }

        [HttpPut("vacation-requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveVacationRequest(long requestId, [FromBody] string approvalNotes = "")
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to approve vacation request by non-admin user");
                return Forbid();
            }

            Log.Information("Approving vacation request {RequestId}", requestId);
            var request = await _context.VacationRequests.FindAsync(requestId);
            
            if (request == null)
                return NotFound();

            // Get current user ID from token
            var authHeader = Request.Headers["Authorization"].ToString();
            var token = authHeader.Substring("Bearer ".Length);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid");
            
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                request.ApprovedByUserId = userId;
            }

            request.Status = "Approved";
            request.ApprovalDate = DateTime.UtcNow;
            request.ApprovalNotes = approvalNotes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("Successfully approved vacation request {RequestId}", requestId);
            return NoContent();
        }

        [HttpPut("vacation-requests/{requestId}/reject")]
        public async Task<IActionResult> RejectVacationRequest(long requestId, [FromBody] string rejectionNotes)
        {
            if (!IsAdmin())
            {
                Log.Warning("Unauthorized attempt to reject vacation request by non-admin user");
                return Forbid();
            }

            Log.Information("Rejecting vacation request {RequestId}", requestId);
            var request = await _context.VacationRequests.FindAsync(requestId);
            
            if (request == null)
                return NotFound();

            request.Status = "Rejected";
            request.ApprovalDate = DateTime.UtcNow;
            request.ApprovalNotes = rejectionNotes;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("Successfully rejected vacation request {RequestId}", requestId);
            return NoContent();
        }

        // === Driver-specific endpoints for transportation industry ===

        [HttpGet("drivers")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetDrivers([FromQuery] bool includeExpiredLicenses = false)
        {
            Log.Information("Fetching drivers (includeExpiredLicenses: {IncludeExpired})", includeExpiredLicenses);
            
            var query = _context.Employees
                .Include(e => e.Job)
                .Include(e => e.Department)
                .Where(e => e.IsActive && !string.IsNullOrEmpty(e.DriverLicenseNumber));

            if (!includeExpiredLicenses)
            {
                var today = DateTime.UtcNow;
                query = query.Where(e => !e.DriverLicenseExpiryDate.HasValue || e.DriverLicenseExpiryDate.Value > today);
            }

            var drivers = await query.ToListAsync();
            
            Log.Debug("Retrieved {DriverCount} drivers", drivers.Count);
            return drivers;
        }

        [HttpGet("expiring-certifications")]
        public async Task<ActionResult<object>> GetExpiringCertifications([FromQuery] int daysAhead = 30)
        {
            Log.Information("Fetching employees with certifications expiring in {DaysAhead} days", daysAhead);
            
            var expiryDate = DateTime.UtcNow.AddDays(daysAhead);
            var today = DateTime.UtcNow;

            var expiringDriverLicenses = await _context.Employees
                .Where(e => e.IsActive && 
                           e.DriverLicenseExpiryDate.HasValue && 
                           e.DriverLicenseExpiryDate.Value >= today &&
                           e.DriverLicenseExpiryDate.Value <= expiryDate)
                .Include(e => e.Job)
                .Select(e => new { 
                    e.EmpId, 
                    e.Name, 
                    e.Surname, 
                    e.Patronym,
                    e.DriverLicenseNumber,
                    ExpiryDate = e.DriverLicenseExpiryDate,
                    Type = "Driver License"
                })
                .ToListAsync();

            var expiringMedicalCerts = await _context.Employees
                .Where(e => e.IsActive && 
                           e.MedicalCertificateExpiryDate.HasValue && 
                           e.MedicalCertificateExpiryDate.Value >= today &&
                           e.MedicalCertificateExpiryDate.Value <= expiryDate)
                .Include(e => e.Job)
                .Select(e => new { 
                    e.EmpId, 
                    e.Name, 
                    e.Surname, 
                    e.Patronym,
                    CertificateNumber = e.MedicalCertificateNumber,
                    ExpiryDate = e.MedicalCertificateExpiryDate,
                    Type = "Medical Certificate"
                })
                .ToListAsync();

            var expiringTrainings = await _context.EmployeeTrainings
                .Where(t => t.ExpiryDate.HasValue && 
                           t.ExpiryDate.Value >= today &&
                           t.ExpiryDate.Value <= expiryDate &&
                           t.IsMandatory)
                .Include(t => t.Employee)
                .Select(t => new {
                    t.Employee.EmpId,
                    t.Employee.Name,
                    t.Employee.Surname,
                    t.Employee.Patronym,
                    t.TrainingName,
                    t.ExpiryDate,
                    Type = "Training"
                })
                .ToListAsync();

            var result = new
            {
                ExpiringDriverLicenses = expiringDriverLicenses,
                ExpiringMedicalCertificates = expiringMedicalCerts,
                ExpiringTrainings = expiringTrainings,
                TotalCount = expiringDriverLicenses.Count + expiringMedicalCerts.Count + expiringTrainings.Count
            };

            Log.Debug("Found {TotalCount} expiring certifications", result.TotalCount);
            return result;
        }
    }
}