#if MODERN
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using TicketSalesApp.Core.Data.MongoDB.Documents;

namespace TicketSalesApp.Core.Data.MongoDB
{
    /// <summary>
    /// MongoDB context interface for document database operations
    /// </summary>
    public interface IMongoContext
    {
        IMongoDatabase Database { get; }
        
        // Document collections - alternative main database
        IMongoCollection<UserDocument> Users { get; }
        IMongoCollection<BusDocument> Buses { get; }
        IMongoCollection<RouteDocument> Routes { get; }
        IMongoCollection<TicketDocument> Tickets { get; }
        IMongoCollection<EmployeeDocument> Employees { get; }
        
        // Analytics and logging collections
        IMongoCollection<LogDocument> Logs { get; }
        IMongoCollection<AnalyticsDocument> Analytics { get; }
        IMongoCollection<ExportJobDocument> ExportJobs { get; }
        IMongoCollection<NotificationDocument> Notifications { get; }
        IMongoCollection<AuditLogDocument> AuditLogs { get; }
        
        // Generic collection access
        IMongoCollection<T> GetCollection<T>(string? name = null) where T : BaseDocument;
        
        // Database operations
        Task<bool> TestConnectionAsync();
        Task<string> GetDatabaseInfoAsync();
        Task CreateIndexesAsync();
        Task DropDatabaseAsync();
    }
    
    /// <summary>
    /// MongoDB repository interface for document operations
    /// </summary>
    public interface IMongoRepository<T> where T : BaseDocument
    {
        Task<T?> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter);
        Task<T> InsertAsync(T document);
        Task<IEnumerable<T>> InsertManyAsync(IEnumerable<T> documents);
        Task<bool> UpdateAsync(string id, T document);
        Task<bool> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update);
        Task<bool> DeleteAsync(string id);
        Task<long> DeleteManyAsync(FilterDefinition<T> filter);
        Task<long> CountAsync(FilterDefinition<T>? filter = null);
        Task<bool> ExistsAsync(string id);
        Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null);
        
        // Aggregation operations
        Task<IEnumerable<TResult>> AggregateAsync<TResult>(PipelineDefinition<T, TResult> pipeline);
        Task<IEnumerable<T>> SearchAsync(string searchTerm, params string[] fields);
        
        // Bulk operations
        Task<BulkWriteResult<T>> BulkWriteAsync(IEnumerable<WriteModel<T>> requests);
    }
}
#endif