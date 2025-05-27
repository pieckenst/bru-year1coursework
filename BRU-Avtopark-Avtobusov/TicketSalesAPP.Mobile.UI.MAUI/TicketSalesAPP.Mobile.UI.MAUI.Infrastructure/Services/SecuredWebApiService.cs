using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TicketSalesAPP.Mobile.UI.MAUI.Domain;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;

namespace TicketSalesAPP.Mobile.UI.MAUI.Infrastructure.Services
{
    public class SecuredWebApiService : ISecuredWebApiService
    {
        readonly HttpClient httpClient;
        readonly JsonSerializerOptions jsonSerializerOptions;
        readonly string apiUrl;
        readonly string postEndPointUrl;

        const string ApplicationJson = "application/json";

        public SecuredWebApiService(IPlatformService platformService)
        {
            apiUrl = platformService.GetPlatformValue("https://10.0.2.2:5001/api/", "https://localhost:5001/api/");
            postEndPointUrl = apiUrl + "odata/" + nameof(Post);
            httpClient = new HttpClient(platformService.GetPlatformHttpHandler())
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = SecuredWebApiTypesContext.Default
            };
        }

        public async Task<bool> UserCanDeletePostAsync()
        {
            var jsonString = await httpClient.GetStringAsync($"{apiUrl}CustomEndpoint/CanDeletePost?typeName={nameof(Post)}");
            return JsonSerializer.Deserialize<bool>(jsonString, jsonSerializerOptions);
        }
        public async Task<bool> DeletePostAsync(int postId)
        {
            var response = await httpClient.DeleteAsync($"{postEndPointUrl}({postId})");
            return response.IsSuccessStatusCode;
        }
        public async Task<IEnumerable<Post>?> GetItemsAsync()
        {
            return await RequestItemsAsync($"?$expand=Author($expand=Photo)");
        }


        private async Task<IEnumerable<Post>?> RequestItemsAsync(string? query = null)
        {
            var response = await httpClient.GetStringAsync($"{postEndPointUrl}{query}");
            return JsonSerializer.Deserialize<PostResponse>(response, jsonSerializerOptions)?.Value;
        }

        public async Task<string?> Authenticate(string userName, string password)
        {
            HttpResponseMessage tokenResponse;
            try
            {
                tokenResponse = await RequestTokenAsync(userName, password);
            }
            catch (Exception)
            {
                return "An error occurred when processing the request";
            }
            if (tokenResponse.IsSuccessStatusCode)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await tokenResponse.Content.ReadAsStringAsync());
                return null;
            }
            else
            {
                return await tokenResponse.Content.ReadAsStringAsync();
            }
        }

        private async Task<HttpResponseMessage> RequestTokenAsync(string userName, string password)
        {
            return await httpClient.PostAsync($"{apiUrl}Authentication/Authenticate",
                                                new StringContent(JsonSerializer.Serialize(new { userName, password = $"{password}" }), Encoding.UTF8,
                                                ApplicationJson));
        }

        public async Task<Author?> CurrentUser()
        {
            try
            {
                string stringResponse = await httpClient.GetStringAsync($"{apiUrl}CustomEndpoint/CurrentUser");
                return JsonSerializer.Deserialize<Author>(stringResponse, jsonSerializerOptions);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}