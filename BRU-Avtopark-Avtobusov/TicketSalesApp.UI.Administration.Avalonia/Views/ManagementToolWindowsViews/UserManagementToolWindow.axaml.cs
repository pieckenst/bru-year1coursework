using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views.ManagementToolWindowsViews
{
    public partial class UserManagementToolWindow : UserControl
    {
        public UserManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new UserManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
