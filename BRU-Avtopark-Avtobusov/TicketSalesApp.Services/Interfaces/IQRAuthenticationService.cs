using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.Services.Interfaces
{ 
    public interface IQRAuthenticationService
    {
        Task<string> GenerateQRLoginTokenAsync(User user);
        Task<(bool success, User? user)> ValidateQRLoginTokenAsync(string token);
        Task<string> GenerateQRCodeAsync(User user);
        Task<(string qrCode, string rawData)> GenerateQRCodeWithDataAsync(User user);

        // New methods for direct QR login
        Task<(string qrCode, string rawData)> GenerateDirectLoginQRCodeAsync(string username, string deviceType);
        Task<(bool success, User? user, string deviceId)> ValidateDirectLoginTokenAsync(string token, string deviceType);
        Task<bool> NotifyDeviceLoginSuccessAsync(string deviceId, string token);
    }
}