using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;

namespace TicketSalesAPP.Mobile.UI.MAUI.Infrastructure.Repositories
{
    public class OrdersRepository
    {
        const string CollectionCacheKey = nameof(Order);
        readonly ICacheService cacheService;
        readonly IWebApiService webApiService;

        public OrdersRepository(ICacheService cacheService, IWebApiService webApiService)
        {
            this.cacheService = cacheService;
            this.webApiService = webApiService;
        }

        public Task<IEnumerable<Order>?> GetAsync()
        {
            return cacheService.GetOrCreateAsync(CollectionCacheKey, _ => webApiService.GetItemsAsync());
        }
        public Task<Order?> AddAsync(Order item)
        {
            return webApiService.AddItemAsync(item);
        }
        public async Task<bool> DeleteAsync(Order item)
        {
            var isSuccessful = await webApiService.DeleteItemAsync(item.Id);
            if (isSuccessful)
                cacheService.UpdateCollectionCache(CollectionCacheKey, cachedList => cachedList.Remove(item));
            return isSuccessful;
        }
        public Task<Order?> GetByIdAsync(int id)
        {
            return webApiService.GetItemAsync(id);
        }
        public async Task<bool> UpdateAsync(Order item)
        {
            var result = await webApiService.UpdateItemAsync(item);
            if (result)
            {
                cacheService.UpdateCollectionCache(CollectionCacheKey, cachedList =>
                {
                    int editedItemIndex = ((List<Order>)cachedList).FindIndex(c => c.Id == item.Id);
                    cachedList[editedItemIndex] = item;
                });
            }
            return result;
        }
    }
}