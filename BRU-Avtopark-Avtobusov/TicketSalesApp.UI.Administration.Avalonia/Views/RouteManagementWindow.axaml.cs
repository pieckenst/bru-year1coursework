using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TicketSalesApp.UI.Administration.Avalonia.Views
{
    public partial class RouteManagementWindow : Window
    {
        public RouteManagementWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 