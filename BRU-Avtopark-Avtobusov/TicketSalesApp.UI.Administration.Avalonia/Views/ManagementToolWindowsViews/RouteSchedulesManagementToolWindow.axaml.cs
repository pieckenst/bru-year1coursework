using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views.ManagementToolWindowsViews
{
    public partial class RouteSchedulesManagementToolWindow : UserControl
    {
        public RouteSchedulesManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new RouteSchedulesManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 