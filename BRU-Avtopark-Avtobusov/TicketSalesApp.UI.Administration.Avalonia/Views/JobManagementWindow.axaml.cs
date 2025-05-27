using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TicketSalesApp.UI.Administration.Avalonia.Views
{
    public partial class JobManagementWindow : Window
    {
        public JobManagementWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 