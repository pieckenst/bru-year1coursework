using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Repositories
{
    public interface IOrdersRepository
    {
        public Task<IEnumerable<Order>?> GetAsync();
        public Task<Order?> AddAsync(Order item);
        public Task<bool> DeleteAsync(Order item);
        public Task<Order?> GetByIdAsync(int id);
        public Task<bool> UpdateAsync(Order item);
    }
}