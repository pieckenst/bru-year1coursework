using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TicketSalesApp.UI.Administration.Avalonia.Views
{
    public partial class BusManagementWindow : Window
    {
        public BusManagementWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 