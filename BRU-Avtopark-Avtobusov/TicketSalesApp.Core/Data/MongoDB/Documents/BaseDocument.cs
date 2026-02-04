#if MODERN
using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// Base document class for MongoDB collections
    /// </summary>
    public abstract class BaseDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("version")]
        public int Version { get; set; } = 1;
    }
}
#endif