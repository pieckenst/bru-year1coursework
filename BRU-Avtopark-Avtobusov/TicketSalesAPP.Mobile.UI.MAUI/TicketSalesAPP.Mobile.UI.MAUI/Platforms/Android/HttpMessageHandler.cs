using Javax.Net.Ssl;
using Xamarin.Android.Net;

namespace TicketSalesAPP.Mobile.UI.MAUI
{
    public class HttpMessageHandler : AndroidMessageHandler
    {
        public HttpMessageHandler()
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, errors) => cert is { Issuer: "CN=localhost" } || errors == System.Net.Security.SslPolicyErrors.None;
        }

        protected override IHostnameVerifier GetSSLHostnameVerifier(HttpsURLConnection connection) => new HostnameVerifier();
        private sealed class HostnameVerifier : Java.Lang.Object, IHostnameVerifier
        {
            public bool Verify(string? hostname, ISSLSession? session)
            {
                return HttpsURLConnection.DefaultHostnameVerifier!.Verify(hostname, session) || hostname == "10.0.2.2" && session?.PeerPrincipal?.Name == "CN=localhost";
            }
        }
    }
}