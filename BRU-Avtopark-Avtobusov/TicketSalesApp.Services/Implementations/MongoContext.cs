using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Reflection;
using TicketSalesApp.Core.Data.MongoDB;
using TicketSalesApp.Core.Data.MongoDB.Documents;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// MongoDB context implementation for document database operations
    /// </summary>
    public class MongoContext : IMongoContext
    {
        private readonly IMongoDatabase _database;
        private readonly Dictionary<Type, object> _collections = new();

        public MongoContext(IMongoDatabase database)
        {
            _database = database;
        }

        public IMongoDatabase Database => _database;

        // Document collections - alternative main database
        public IMongoCollection<UserDocument> Users => GetCollection<UserDocument>();
        public IMongoCollection<BusDocument> Buses => GetCollection<BusDocument>();
        public IMongoCollection<RouteDocument> Routes => GetCollection<RouteDocument>();
        public IMongoCollection<TicketDocument> Tickets => GetCollection<TicketDocument>();
        public IMongoCollection<EmployeeDocument> Employees => GetCollection<EmployeeDocument>();

        // Analytics and logging collections
        public IMongoCollection<LogDocument> Logs => GetCollection<LogDocument>();
        public IMongoCollection<AnalyticsDocument> Analytics => GetCollection<AnalyticsDocument>();
        public IMongoCollection<ExportJobDocument> ExportJobs => GetCollection<ExportJobDocument>();
        public IMongoCollection<NotificationDocument> Notifications => GetCollection<NotificationDocument>();
        public IMongoCollection<AuditLogDocument> AuditLogs => GetCollection<AuditLogDocument>();

        public IMongoCollection<T> GetCollection<T>(string? name = null) where T : BaseDocument
        {
            var type = typeof(T);
            
            if (_collections.TryGetValue(type, out var cachedCollection))
            {
                return (IMongoCollection<T>)cachedCollection;
            }

            var collectionName = name ?? GetCollectionName<T>();
            var collection = _database.GetCollection<T>(collectionName);
            
            _collections[type] = collection;
            return collection;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Try to ping the database
                await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetDatabaseInfoAsync()
        {
            try
            {
                var stats = await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("dbStats", 1));
                return stats.ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting database info: {ex.Message}";
            }
        }

        public async Task CreateIndexesAsync()
        {
            try
            {
                // Create indexes for Users collection
                var userIndexes = new[]
                {
                    new CreateIndexModel<UserDocument>(
                        Builders<UserDocument>.IndexKeys.Ascending(u => u.UserId),
                        new CreateIndexOptions { Unique = true, Name = "userId_unique" }
                    ),
                    new CreateIndexModel<UserDocument>(
                        Builders<UserDocument>.IndexKeys.Ascending(u => u.GuidId),
                        new CreateIndexOptions { Unique = true, Name = "guidId_unique" }
                    ),
                    new CreateIndexModel<UserDocument>(
                        Builders<UserDocument>.IndexKeys.Ascending(u => u.Login),
                        new CreateIndexOptions { Unique = true, Name = "login_unique" }
                    ),
                    new CreateIndexModel<UserDocument>(
                        Builders<UserDocument>.IndexKeys.Ascending(u => u.Email),
                        new CreateIndexOptions { Name = "email_index" }
                    )
                };
                await Users.Indexes.CreateManyAsync(userIndexes);

                // Create indexes for Buses collection
                var busIndexes = new[]
                {
                    new CreateIndexModel<BusDocument>(
                        Builders<BusDocument>.IndexKeys.Ascending(b => b.BusId),
                        new CreateIndexOptions { Unique = true, Name = "busId_unique" }
                    ),
                    new CreateIndexModel<BusDocument>(
                        Builders<BusDocument>.IndexKeys.Ascending(b => b.BusNumber),
                        new CreateIndexOptions { Name = "busNumber_index" }
                    ),
                    new CreateIndexModel<BusDocument>(
                        Builders<BusDocument>.IndexKeys.Ascending(b => b.IsActive),
                        new CreateIndexOptions { Name = "isActive_index" }
                    )
                };
                await Buses.Indexes.CreateManyAsync(busIndexes);

                // Create indexes for Routes collection
                var routeIndexes = new[]
                {
                    new CreateIndexModel<RouteDocument>(
                        Builders<RouteDocument>.IndexKeys.Ascending(r => r.RouteId),
                        new CreateIndexOptions { Unique = true, Name = "routeId_unique" }
                    ),
                    new CreateIndexModel<RouteDocument>(
                        Builders<RouteDocument>.IndexKeys.Ascending(r => r.RouteName),
                        new CreateIndexOptions { Name = "routeName_index" }
                    ),
                    new CreateIndexModel<RouteDocument>(
                        Builders<RouteDocument>.IndexKeys.Ascending(r => r.IsActive),
                        new CreateIndexOptions { Name = "isActive_index" }
                    )
                };
                await Routes.Indexes.CreateManyAsync(routeIndexes);

                // Create indexes for Tickets collection
                var ticketIndexes = new[]
                {
                    new CreateIndexModel<TicketDocument>(
                        Builders<TicketDocument>.IndexKeys.Ascending(t => t.TicketId),
                        new CreateIndexOptions { Unique = true, Name = "ticketId_unique" }
                    ),
                    new CreateIndexModel<TicketDocument>(
                        Builders<TicketDocument>.IndexKeys.Ascending(t => t.RouteId),
                        new CreateIndexOptions { Name = "routeId_index" }
                    ),
                    new CreateIndexModel<TicketDocument>(
                        Builders<TicketDocument>.IndexKeys.Ascending(t => t.IsActive),
                        new CreateIndexOptions { Name = "isActive_index" }
                    )
                };
                await Tickets.Indexes.CreateManyAsync(ticketIndexes);

                // Create indexes for Employees collection
                var employeeIndexes = new[]
                {
                    new CreateIndexModel<EmployeeDocument>(
                        Builders<EmployeeDocument>.IndexKeys.Ascending(e => e.EmployeeId),
                        new CreateIndexOptions { Unique = true, Name = "employeeId_unique" }
                    ),
                    new CreateIndexModel<EmployeeDocument>(
                        Builders<EmployeeDocument>.IndexKeys.Ascending(e => e.Email),
                        new CreateIndexOptions { Name = "email_index" }
                    ),
                    new CreateIndexModel<EmployeeDocument>(
                        Builders<EmployeeDocument>.IndexKeys.Ascending(e => e.IsActive),
                        new CreateIndexOptions { Name = "isActive_index" }
                    )
                };
                await Employees.Indexes.CreateManyAsync(employeeIndexes);

                // Create indexes for Logs collection
                var logIndexes = new[]
                {
                    new CreateIndexModel<LogDocument>(
                        Builders<LogDocument>.IndexKeys.Descending(l => l.Timestamp),
                        new CreateIndexOptions { Name = "timestamp_desc" }
                    ),
                    new CreateIndexModel<LogDocument>(
                        Builders<LogDocument>.IndexKeys.Ascending(l => l.Level),
                        new CreateIndexOptions { Name = "level_index" }
                    ),
                    new CreateIndexModel<LogDocument>(
                        Builders<LogDocument>.IndexKeys.Ascending(l => l.UserId),
                        new CreateIndexOptions { Name = "userId_index" }
                    ),
                    new CreateIndexModel<LogDocument>(
                        Builders<LogDocument>.IndexKeys.Ascending(l => l.CorrelationId),
                        new CreateIndexOptions { Name = "correlationId_index" }
                    )
                };
                await Logs.Indexes.CreateManyAsync(logIndexes);

                // Create indexes for Analytics collection
                var analyticsIndexes = new[]
                {
                    new CreateIndexModel<AnalyticsDocument>(
                        Builders<AnalyticsDocument>.IndexKeys.Descending(a => a.Timestamp),
                        new CreateIndexOptions { Name = "timestamp_desc" }
                    ),
                    new CreateIndexModel<AnalyticsDocument>(
                        Builders<AnalyticsDocument>.IndexKeys.Ascending(a => a.EventType),
                        new CreateIndexOptions { Name = "eventType_index" }
                    ),
                    new CreateIndexModel<AnalyticsDocument>(
                        Builders<AnalyticsDocument>.IndexKeys.Ascending(a => a.UserId),
                        new CreateIndexOptions { Name = "userId_index" }
                    )
                };
                await Analytics.Indexes.CreateManyAsync(analyticsIndexes);

                // Create indexes for ExportJobs collection
                var exportJobIndexes = new[]
                {
                    new CreateIndexModel<ExportJobDocument>(
                        Builders<ExportJobDocument>.IndexKeys.Ascending(e => e.JobId),
                        new CreateIndexOptions { Unique = true, Name = "jobId_unique" }
                    ),
                    new CreateIndexModel<ExportJobDocument>(
                        Builders<ExportJobDocument>.IndexKeys.Ascending(e => e.UserId),
                        new CreateIndexOptions { Name = "userId_index" }
                    ),
                    new CreateIndexModel<ExportJobDocument>(
                        Builders<ExportJobDocument>.IndexKeys.Ascending(e => e.Status),
                        new CreateIndexOptions { Name = "status_index" }
                    ),
                    new CreateIndexModel<ExportJobDocument>(
                        Builders<ExportJobDocument>.IndexKeys.Ascending(e => e.ExpiresAt),
                        new CreateIndexOptions { Name = "expiresAt_index" }
                    )
                };
                await ExportJobs.Indexes.CreateManyAsync(exportJobIndexes);

                // Create indexes for Notifications collection
                var notificationIndexes = new[]
                {
                    new CreateIndexModel<NotificationDocument>(
                        Builders<NotificationDocument>.IndexKeys.Ascending(n => n.NotificationId),
                        new CreateIndexOptions { Unique = true, Name = "notificationId_unique" }
                    ),
                    new CreateIndexModel<NotificationDocument>(
                        Builders<NotificationDocument>.IndexKeys.Ascending(n => n.UserId),
                        new CreateIndexOptions { Name = "userId_index" }
                    ),
                    new CreateIndexModel<NotificationDocument>(
                        Builders<NotificationDocument>.IndexKeys.Ascending(n => n.Status),
                        new CreateIndexOptions { Name = "status_index" }
                    ),
                    new CreateIndexModel<NotificationDocument>(
                        Builders<NotificationDocument>.IndexKeys.Descending(n => n.CreatedAt),
                        new CreateIndexOptions { Name = "createdAt_desc" }
                    )
                };
                await Notifications.Indexes.CreateManyAsync(notificationIndexes);

                // Create indexes for AuditLogs collection
                var auditLogIndexes = new[]
                {
                    new CreateIndexModel<AuditLogDocument>(
                        Builders<AuditLogDocument>.IndexKeys.Ascending(a => a.AuditId),
                        new CreateIndexOptions { Unique = true, Name = "auditId_unique" }
                    ),
                    new CreateIndexModel<AuditLogDocument>(
                        Builders<AuditLogDocument>.IndexKeys.Descending(a => a.Timestamp),
                        new CreateIndexOptions { Name = "timestamp_desc" }
                    ),
                    new CreateIndexModel<AuditLogDocument>(
                        Builders<AuditLogDocument>.IndexKeys.Ascending(a => a.UserId),
                        new CreateIndexOptions { Name = "userId_index" }
                    ),
                    new CreateIndexModel<AuditLogDocument>(
                        Builders<AuditLogDocument>.IndexKeys.Ascending(a => a.EntityType),
                        new CreateIndexOptions { Name = "entityType_index" }
                    ),
                    new CreateIndexModel<AuditLogDocument>(
                        Builders<AuditLogDocument>.IndexKeys.Ascending(a => a.Action),
                        new CreateIndexOptions { Name = "action_index" }
                    )
                };
                await AuditLogs.Indexes.CreateManyAsync(auditLogIndexes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create MongoDB indexes: {ex.Message}", ex);
            }
        }

        public async Task DropDatabaseAsync()
        {
            await _database.Client.DropDatabaseAsync(_database.DatabaseNamespace.DatabaseName);
        }

        /// <summary>
        /// Gets the collection name from the BsonCollection attribute or uses the class name
        /// </summary>
        private string GetCollectionName<T>() where T : BaseDocument
        {
            var attribute = typeof(T).GetCustomAttribute<BsonCollectionAttribute>();
            return attribute?.CollectionName ?? typeof(T).Name.ToLowerInvariant();
        }
    }
}