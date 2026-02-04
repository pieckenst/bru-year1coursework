using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Services.Implementations.DatabaseProviders
{
    /// <summary>
    /// Factory for creating database providers based on provider name
    /// </summary>
    public class DatabaseProviderFactory : IDatabaseProviderFactory
    {
        private readonly Func<DbContextOptions<AppDbContext>, AppDbContext> _contextFactory;
        private readonly IMongoDatabase? _mongoDatabase;
        private readonly IMongoContext? _mongoContext;

        public DatabaseProviderFactory(
            Func<DbContextOptions<AppDbContext>, AppDbContext> contextFactory,
            IMongoDatabase? mongoDatabase = null,
            IMongoContext? mongoContext = null)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _mongoDatabase = mongoDatabase;
            _mongoContext = mongoContext;
        }

        public IDatabaseProvider CreateProvider(string providerName, string connectionString)
        {
            return providerName.ToLowerInvariant() switch
            {
                "sqlite" => new SqliteDatabaseProvider(connectionString, _contextFactory),
                "sqlserver" => new SqlServerDatabaseProvider(connectionString, _contextFactory),
                "postgresql" => new PostgreSqlDatabaseProvider(connectionString, _contextFactory),
                "mongodb" => CreateMongoDbProvider(connectionString),
                _ => throw new NotSupportedException($"Database provider '{providerName}' is not supported")
            };
        }

        public IEnumerable<string> GetSupportedProviders()
        {
            return new[] { "SQLite", "SqlServer", "PostgreSQL", "MongoDB" };
        }

        private IDatabaseProvider CreateMongoDbProvider(string connectionString)
        {
            if (_mongoDatabase == null || _mongoContext == null)
            {
                throw new InvalidOperationException("MongoDB dependencies not configured. MongoDB is disabled in the current configuration. To enable MongoDB, set Database:EnableMongoDB to true or set Database:Provider to MongoDB in appsettings.json.");
            }

            return new MongoDbDatabaseProvider(connectionString, _mongoDatabase, _mongoContext);
        }
    }
}