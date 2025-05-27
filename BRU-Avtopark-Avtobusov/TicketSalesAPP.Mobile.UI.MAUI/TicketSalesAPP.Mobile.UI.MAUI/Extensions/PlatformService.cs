using DevExpress.Maui.Core;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;

namespace TicketSalesAPP.Mobile.UI.MAUI.Extensions
{
    public class PlatformService : IPlatformService
    {
        public System.Net.Http.HttpMessageHandler GetPlatformHttpHandler()
        {
            return new HttpMessageHandler();
        }

        public TValue GetPlatformValue<TValue>(TValue android, TValue ios)
        {
            return ON.Platform(android, ios);
        }
    }
}