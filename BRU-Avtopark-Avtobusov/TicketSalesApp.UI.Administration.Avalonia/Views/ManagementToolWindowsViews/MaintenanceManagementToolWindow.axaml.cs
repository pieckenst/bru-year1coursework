using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views.ManagementToolWindowsViews
{
    public partial class MaintenanceManagementToolWindow : UserControl
    {
        public MaintenanceManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new MaintenanceManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}