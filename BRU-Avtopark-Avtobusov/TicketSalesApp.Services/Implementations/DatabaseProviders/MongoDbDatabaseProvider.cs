using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Services.Implementations.DatabaseProviders
{
    /// <summary>
    /// MongoDB database provider implementation
    /// This allows MongoDB to be used as an alternative main database option
    /// </summary>
    public class MongoDbDatabaseProvider : IDatabaseProvider
    {
        private readonly string _connectionString;
        private readonly IMongoDatabase _database;
        private readonly IMongoContext _mongoContext;

        public MongoDbDatabaseProvider(string connectionString, IMongoDatabase database, IMongoContext mongoContext)
        {
            _connectionString = connectionString;
            _database = database;
            _mongoContext = mongoContext;
        }

        public string ProviderName => "MongoDB";
        public string ConnectionString => _connectionString;

        public DbContext CreateContext()
        {
            // MongoDB doesn't use Entity Framework, so we return a mock context
            // In practice, you would use the IMongoContext instead
            throw new NotSupportedException("MongoDB does not use Entity Framework DbContext. Use IMongoContext instead.");
        }

        public async Task MigrateAsync()
        {
            // MongoDB doesn't require migrations like SQL databases
            // Instead, we create indexes and ensure collections exist
            await _mongoContext.CreateIndexesAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await _mongoContext.TestConnectionAsync();
        }

        public async Task<string> GetVersionAsync()
        {
            try
            {
                var buildInfo = await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("buildInfo", 1));
                return buildInfo.GetValue("version", "Unknown").ToString();
            }
            catch (Exception ex)
            {
                return $"Error getting version: {ex.Message}";
            }
        }

        public async Task<Dictionary<string, object>> GetHealthInfoAsync()
        {
            var healthInfo = new Dictionary<string, object>
            {
                ["Provider"] = ProviderName,
                ["ConnectionString"] = MaskConnectionString(_connectionString)
            };

            try
            {
                var canConnect = await TestConnectionAsync();
                healthInfo["CanConnect"] = canConnect;

                if (canConnect)
                {
                    // Get MongoDB version and build info
                    var buildInfo = await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("buildInfo", 1));
                    healthInfo["Version"] = buildInfo.GetValue("version", "Unknown").ToString();
                    healthInfo["GitVersion"] = buildInfo.GetValue("gitVersion", "Unknown").ToString();

                    // Get database stats
                    var dbStats = await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("dbStats", 1));
                    healthInfo["DatabaseName"] = _database.DatabaseNamespace.DatabaseName;
                    healthInfo["Collections"] = dbStats.GetValue("collections", 0).ToInt32();
                    healthInfo["Objects"] = dbStats.GetValue("objects", 0).ToInt64();
                    healthInfo["DataSizeBytes"] = dbStats.GetValue("dataSize", 0).ToInt64();
                    healthInfo["DataSizeMB"] = Math.Round(dbStats.GetValue("dataSize", 0).ToInt64() / 1024.0 / 1024.0, 2);
                    healthInfo["StorageSizeBytes"] = dbStats.GetValue("storageSize", 0).ToInt64();
                    healthInfo["StorageSizeMB"] = Math.Round(dbStats.GetValue("storageSize", 0).ToInt64() / 1024.0 / 1024.0, 2);
                    healthInfo["IndexSizeBytes"] = dbStats.GetValue("indexSize", 0).ToInt64();
                    healthInfo["IndexSizeMB"] = Math.Round(dbStats.GetValue("indexSize", 0).ToInt64() / 1024.0 / 1024.0, 2);

                    // Get server status
                    var serverStatus = await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("serverStatus", 1));
                    if (serverStatus.Contains("host"))
                    {
                        healthInfo["Host"] = serverStatus.GetValue("host", "Unknown").ToString();
                    }
                    if (serverStatus.Contains("uptime"))
                    {
                        healthInfo["UptimeSeconds"] = serverStatus.GetValue("uptime", 0).ToInt64();
                    }

                    // Get collection information
                    var collectionNames = await _database.ListCollectionNamesAsync();
                    var collections = await collectionNames.ToListAsync();
                    healthInfo["CollectionNames"] = collections;

                    // Get document counts for main collections
                    var documentCounts = new Dictionary<string, long>();
                    foreach (var collectionName in collections.Take(10)) // Limit to first 10 collections
                    {
                        try
                        {
                            var collection = _database.GetCollection<MongoDB.Bson.BsonDocument>(collectionName);
                            var count = await collection.CountDocumentsAsync(new MongoDB.Bson.BsonDocument());
                            documentCounts[collectionName] = count;
                        }
                        catch
                        {
                            // Skip collections that can't be counted
                        }
                    }
                    healthInfo["DocumentCounts"] = documentCounts;
                }
            }
            catch (Exception ex)
            {
                healthInfo["Error"] = ex.Message;
                healthInfo["CanConnect"] = false;
            }

            return healthInfo;
        }

        private static string MaskConnectionString(string connectionString)
        {
            // Mask sensitive information in MongoDB connection string
            var masked = connectionString;
            
            // Mask password in MongoDB connection string format
            // mongodb://username:password@host:port/database
            masked = System.Text.RegularExpressions.Regex.Replace(masked, @"://([^:]+):([^@]+)@", "://$1:***@");
            
            // Also handle connection string parameters
            masked = System.Text.RegularExpressions.Regex.Replace(masked, @"password=[^&]*", "password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            return masked;
        }
    }
}