using Microsoft.Extensions.Caching.Memory;
using System.Collections;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Services
{
    public interface ICacheService
    {
        TItem? GetOrCreate<TItem>(string cacheKey, Func<ICacheEntry, TItem> factory);
        Task<TItem?> GetOrCreateAsync<TItem>(string cacheKey, Func<ICacheEntry, Task<TItem>> factory);
        void UpdateCollectionCache(string collectionKey, Action<IList> updateAction);
        void ClearCollectionCache(string collectionKey);
    }
}