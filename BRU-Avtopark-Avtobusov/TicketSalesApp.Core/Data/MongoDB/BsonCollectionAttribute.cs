#if MODERN
using System;

namespace TicketSalesApp.Core.Data.MongoDB
{
    /// <summary>
    /// Attribute to specify MongoDB collection name for document classes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BsonCollectionAttribute : Attribute
    {
        public string CollectionName { get; }

        public BsonCollectionAttribute(string collectionName)
        {
            CollectionName = collectionName;
        }
    }
}
#endif