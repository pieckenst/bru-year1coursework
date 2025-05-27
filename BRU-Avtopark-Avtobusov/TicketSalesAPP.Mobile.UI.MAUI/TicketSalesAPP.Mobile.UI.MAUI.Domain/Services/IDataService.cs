using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Services
{
    public interface IDataService
    {
        Task<IEnumerable<Customer>> GetCustomersAsync();
    }
}