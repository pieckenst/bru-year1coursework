using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TicketSalesApp.UI.Avalonia.Views.Pages
{
    public partial class MyTicketsView : UserControl
    {
        public MyTicketsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 