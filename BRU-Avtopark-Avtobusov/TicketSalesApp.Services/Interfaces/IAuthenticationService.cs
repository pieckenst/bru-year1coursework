// Services/Interfaces/IAuthenticationService.cs
using System.Threading.Tasks;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<User?> AuthenticateAsync(string login, string password);
        Task<bool> RegisterAsync(string login, string password, int role);
        Task<User?> AuthenticateDirectQRAsync(string login, string validationToken);
    }
}