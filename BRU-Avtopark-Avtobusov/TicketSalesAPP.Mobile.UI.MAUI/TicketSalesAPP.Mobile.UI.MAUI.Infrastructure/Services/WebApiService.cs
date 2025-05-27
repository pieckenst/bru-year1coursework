using System.Net.Http.Json;
using System.Text.Json;
using TicketSalesAPP.Mobile.UI.MAUI.Domain;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;

namespace TicketSalesAPP.Mobile.UI.MAUI.Infrastructure.Services
{
    public class WebApiService : IWebApiService
    {
        readonly HttpClient httpClient;
        readonly JsonSerializerOptions serializerOptions;

        public WebApiService(IPlatformService platformService)
        {
            httpClient = new HttpClient()
            {
                BaseAddress = new Uri(platformService.GetPlatformValue("http://10.0.2.2:5072/api/", "http://localhost:5072/api/"))
            };
            serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = WebApiTypesContext.Default
            };
        }

        public Task<IEnumerable<Order>?> GetItemsAsync()
        {
            return httpClient.GetFromJsonAsync<IEnumerable<Order>>("orders", serializerOptions);
        }
        public Task<bool> DeleteItemAsync(int id)
        {
            return Task.Run(async () =>
            {
                var response = await httpClient.DeleteAsync($"orders/{id}");
                return response.IsSuccessStatusCode;
            });
        }
        public Task<Order?> GetItemAsync(int id)
        {
            return httpClient.GetFromJsonAsync<Order>($"orders/{id}", serializerOptions);
        }
        public async Task<Order?> AddItemAsync(Order item)
        {
            var response = await httpClient.PostAsJsonAsync("orders", item, serializerOptions);
            return await response.Content.ReadFromJsonAsync<Order>(serializerOptions);
        }
        public async Task<bool> UpdateItemAsync(Order item)
        {
            var response = await httpClient.PutAsJsonAsync($"orders/{item.Id}", item, serializerOptions);
            return response.IsSuccessStatusCode;
        }
    }
}