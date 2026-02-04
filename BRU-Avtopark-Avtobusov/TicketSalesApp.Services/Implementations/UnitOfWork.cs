using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// Unit of Work implementation for managing transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // Repository instances
        private IRepository<User>? _users;
        private IRepository<Employee>? _employees;
        private IRepository<Job>? _jobs;
        private IRepository<Avtobus>? _buses;
        private IRepository<Marshut>? _routes;
        private IRepository<Bilet>? _tickets;
        private IRepository<Prodazha>? _sales;
        private IRepository<Obsluzhivanie>? _maintenance;
        private IRepository<AdminActionLog>? _adminActionLogs;
        private IRepository<RouteSchedules>? _routeSchedules;
        private IRepository<FormDefinition>? _formDefinitions;
        private IRepository<WebAuthnCredential>? _webAuthnCredentials;
        
        // RBAC repositories
        private IRepository<Roles>? _roles;
        private IRepository<Permission>? _permissions;
        private IRepository<UserRole>? _userRoles;
        private IRepository<RolePermission>? _rolePermissions;
        
        // HR repositories
        private IRepository<Department>? _departments;
        private IRepository<EmployeeDocument>? _employeeDocuments;
        private IRepository<EmployeeTraining>? _employeeTrainings;
        private IRepository<EmergencyContact>? _emergencyContacts;
        private IRepository<VacationRequest>? _vacationRequests;

        public UnitOfWork(DbContext context)
        {
            _context = context;
        }

        // Entity repositories
        public IRepository<User> Users => _users ??= new Repository<User>(_context);
        public IRepository<Employee> Employees => _employees ??= new Repository<Employee>(_context);
        public IRepository<Job> Jobs => _jobs ??= new Repository<Job>(_context);
        public IRepository<Avtobus> Buses => _buses ??= new Repository<Avtobus>(_context);
        public IRepository<Marshut> Routes => _routes ??= new Repository<Marshut>(_context);
        public IRepository<Bilet> Tickets => _tickets ??= new Repository<Bilet>(_context);
        public IRepository<Prodazha> Sales => _sales ??= new Repository<Prodazha>(_context);
        public IRepository<Obsluzhivanie> Maintenance => _maintenance ??= new Repository<Obsluzhivanie>(_context);
        public IRepository<AdminActionLog> AdminActionLogs => _adminActionLogs ??= new Repository<AdminActionLog>(_context);
        public IRepository<RouteSchedules> RouteSchedules => _routeSchedules ??= new Repository<RouteSchedules>(_context);
        public IRepository<FormDefinition> FormDefinitions => _formDefinitions ??= new Repository<FormDefinition>(_context);
        public IRepository<WebAuthnCredential> WebAuthnCredentials => _webAuthnCredentials ??= new Repository<WebAuthnCredential>(_context);

        // RBAC repositories
        public IRepository<Roles> Roles => _roles ??= new Repository<Roles>(_context);
        public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);
        public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
        public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);

        // HR repositories
        public IRepository<Department> Departments => _departments ??= new Repository<Department>(_context);
        public IRepository<EmployeeDocument> EmployeeDocuments => _employeeDocuments ??= new Repository<EmployeeDocument>(_context);
        public IRepository<EmployeeTraining> EmployeeTrainings => _employeeTrainings ??= new Repository<EmployeeTraining>(_context);
        public IRepository<EmergencyContact> EmergencyContacts => _emergencyContacts ??= new Repository<EmergencyContact>(_context);
        public IRepository<VacationRequest> VacationRequests => _vacationRequests ??= new Repository<VacationRequest>(_context);

        // Transaction management
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                throw new InvalidOperationException("Transaction already started");

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to commit");

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await _transaction.RollbackAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to rollback");

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        // Database health
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetDatabaseProviderAsync()
        {
            return await Task.FromResult(_context.Database.ProviderName ?? "Unknown");
        }

        // Dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context?.Dispose();
                _disposed = true;
            }
        }
    }
}