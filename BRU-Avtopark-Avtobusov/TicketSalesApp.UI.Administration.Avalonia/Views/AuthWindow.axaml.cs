// UI/Avalonia/Views/AuthWindow.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views;

public partial class AuthWindow : Window
{
    public AuthWindow()
    {
        InitializeComponent();
        DataContext = new AuthViewModel();
        Log.Debug("AuthWindow initialized");
    }

    private async void ShowErrorMessage(string message)
    {
        var msBoxStandardWindow = MessageBoxManager
            .GetMessageBoxStandard("Error", message,
                ButtonEnum.Ok,
                MsBox.Avalonia.Enums.Icon.Error);

        await msBoxStandardWindow.ShowAsync();
    }
}
