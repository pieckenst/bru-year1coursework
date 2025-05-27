using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;

namespace TicketSalesAPP.Mobile.UI.MAUI.Infrastructure.Services
{
    public class DataService : IDataService
    {
        public async Task<IEnumerable<Customer>> GetCustomersAsync()
        {
            await Task.Delay(2500);
            return Enumerable.Range(1, 8).Select(x => new Customer(x)).ToList();
        }
    }
}