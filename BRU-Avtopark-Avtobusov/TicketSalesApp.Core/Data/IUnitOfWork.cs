#if MODERN
using TicketSalesApp.Core.Models;

using System;
using System.Threading.Tasks;

namespace TicketSalesApp.Core.Data
{
    /// <summary>
    /// Unit of Work pattern interface for managing transactions and repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Entity repositories
        IRepository<User> Users { get; }
        IRepository<Employee> Employees { get; }
        IRepository<Job> Jobs { get; }
        IRepository<Avtobus> Buses { get; }
        IRepository<Marshut> Routes { get; }
        IRepository<Bilet> Tickets { get; }
        IRepository<Prodazha> Sales { get; }
        IRepository<Obsluzhivanie> Maintenance { get; }
        IRepository<AdminActionLog> AdminActionLogs { get; }
        IRepository<RouteSchedules> RouteSchedules { get; }
        IRepository<FormDefinition> FormDefinitions { get; }
        IRepository<WebAuthnCredential> WebAuthnCredentials { get; }
        
        // RBAC repositories
        IRepository<Roles> Roles { get; }
        IRepository<Permission> Permissions { get; }
        IRepository<UserRole> UserRoles { get; }
        IRepository<RolePermission> RolePermissions { get; }
        
        // HR repositories
        IRepository<Department> Departments { get; }
        IRepository<EmployeeDocument> EmployeeDocuments { get; }
        IRepository<EmployeeTraining> EmployeeTrainings { get; }
        IRepository<EmergencyContact> EmergencyContacts { get; }
        IRepository<VacationRequest> VacationRequests { get; }
        
        // Transaction management
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        
        // Database health
        Task<bool> TestConnectionAsync();
        Task<string> GetDatabaseProviderAsync();
    }
}
#endif