
namespace TicketSalesAPP.Mobile.UI.MAUI
{
    public class HttpMessageHandler : NSUrlSessionHandler
    {
        public HttpMessageHandler()
        {
            ServerCertificateCustomValidationCallback += (_, cert, _, errors) => cert is { Issuer: "CN=localhost" } || errors == System.Net.Security.SslPolicyErrors.None;
            TrustOverrideForUrl = (sender, url, trust) => true;
        }
    }
}