
namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Services
{
    public interface IPlatformService
    {
        HttpMessageHandler GetPlatformHttpHandler();
        TValue GetPlatformValue<TValue>(TValue android, TValue ios);
    }
}