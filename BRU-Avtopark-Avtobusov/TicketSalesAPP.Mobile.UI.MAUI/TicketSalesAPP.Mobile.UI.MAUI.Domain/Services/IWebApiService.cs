using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Services
{
    public interface IWebApiService
    {
        Task<IEnumerable<Order>?> GetItemsAsync();
        Task<bool> DeleteItemAsync(int id);
        Task<Order?> GetItemAsync(int id);
        Task<Order?> AddItemAsync(Order item);
        Task<bool> UpdateItemAsync(Order item);
    }
}