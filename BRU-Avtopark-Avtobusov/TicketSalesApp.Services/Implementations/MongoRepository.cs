using MongoDB.Driver;
using System.Reflection;
using TicketSalesApp.Core.Data.MongoDB;
using TicketSalesApp.Core.Data.MongoDB.Documents;

namespace TicketSalesApp.Services.Implementations
{
    /// <summary>
    /// MongoDB repository implementation for document operations
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    public class MongoRepository<T> : IMongoRepository<T> where T : BaseDocument
    {
        private readonly IMongoCollection<T> _collection;

        public MongoRepository(IMongoDatabase database)
        {
            var collectionName = GetCollectionName();
            _collection = database.GetCollection<T>(collectionName);
        }

        public MongoRepository(IMongoCollection<T> collection)
        {
            _collection = collection;
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq(doc => doc.Id, id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(FilterDefinition<T> filter)
        {
            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<T> InsertAsync(T document)
        {
            document.CreatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
            
            await _collection.InsertOneAsync(document);
            return document;
        }

        public async Task<IEnumerable<T>> InsertManyAsync(IEnumerable<T> documents)
        {
            var documentsToInsert = documents.ToList();
            var now = DateTime.UtcNow;
            
            foreach (var document in documentsToInsert)
            {
                document.CreatedAt = now;
                document.UpdatedAt = now;
            }
            
            await _collection.InsertManyAsync(documentsToInsert);
            return documentsToInsert;
        }

        public async Task<bool> UpdateAsync(string id, T document)
        {
            document.UpdatedAt = DateTime.UtcNow;
            document.Version++;
            
            var filter = Builders<T>.Filter.Eq(doc => doc.Id, id);
            var result = await _collection.ReplaceOneAsync(filter, document);
            
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            var updateWithTimestamp = Builders<T>.Update
                .Combine(update, Builders<T>.Update.Set(doc => doc.UpdatedAt, DateTime.UtcNow));
            
            var result = await _collection.UpdateManyAsync(filter, updateWithTimestamp);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq(doc => doc.Id, id);
            var result = await _collection.DeleteOneAsync(filter);
            
            return result.DeletedCount > 0;
        }

        public async Task<long> DeleteManyAsync(FilterDefinition<T> filter)
        {
            var result = await _collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }

        public async Task<long> CountAsync(FilterDefinition<T>? filter = null)
        {
            filter ??= Builders<T>.Filter.Empty;
            return await _collection.CountDocumentsAsync(filter);
        }

        public async Task<bool> ExistsAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq(doc => doc.Id, id);
            var count = await _collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });
            
            return count > 0;
        }

        public async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, FilterDefinition<T>? filter = null, SortDefinition<T>? sort = null)
        {
            filter ??= Builders<T>.Filter.Empty;
            sort ??= Builders<T>.Sort.Descending(doc => doc.CreatedAt);
            
            return await _collection
                .Find(filter)
                .Sort(sort)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<TResult>> AggregateAsync<TResult>(PipelineDefinition<T, TResult> pipeline)
        {
            return await _collection.Aggregate(pipeline).ToListAsync();
        }

        public async Task<IEnumerable<T>> SearchAsync(string searchTerm, params string[] fields)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || fields.Length == 0)
                return await GetAllAsync();

            var filterBuilder = Builders<T>.Filter;
            var filters = new List<FilterDefinition<T>>();

            foreach (var field in fields)
            {
                var regex = new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"); // Case-insensitive
                filters.Add(filterBuilder.Regex(field, regex));
            }

            var combinedFilter = filterBuilder.Or(filters);
            return await FindAsync(combinedFilter);
        }

        public async Task<BulkWriteResult<T>> BulkWriteAsync(IEnumerable<WriteModel<T>> requests)
        {
            return await _collection.BulkWriteAsync(requests);
        }

        /// <summary>
        /// Gets the collection name from the BsonCollection attribute or uses the class name
        /// </summary>
        private string GetCollectionName()
        {
            var attribute = typeof(T).GetCustomAttribute<BsonCollectionAttribute>();
            return attribute?.CollectionName ?? typeof(T).Name.ToLowerInvariant();
        }
    }
}