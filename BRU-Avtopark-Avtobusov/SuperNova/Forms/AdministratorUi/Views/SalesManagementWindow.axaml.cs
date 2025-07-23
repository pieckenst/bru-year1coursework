using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;

namespace SuperNova.Forms.AdministratorUi.Views
{
    public partial class SalesManagementWindow : PleasantWindow
    {
        public SalesManagementWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 