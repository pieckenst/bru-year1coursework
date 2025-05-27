using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TicketSalesApp.UI.Avalonia.Views.Pages
{
    public partial class SearchResultsView : UserControl
    {
        public SearchResultsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 