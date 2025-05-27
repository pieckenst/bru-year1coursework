using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;

namespace TicketSalesAPP.Mobile.UI.MAUI.Domain.Services
{
    public interface ISecuredWebApiService
    {
        Task<IEnumerable<Post>?> GetItemsAsync();
        Task<bool> UserCanDeletePostAsync();
        Task<bool> DeletePostAsync(int postId);
        Task<string?> Authenticate(string userName, string password);
        Task<Author?> CurrentUser();
    }
}
