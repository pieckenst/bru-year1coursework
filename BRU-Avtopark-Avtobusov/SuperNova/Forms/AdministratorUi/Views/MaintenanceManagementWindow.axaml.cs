using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;

namespace SuperNova.Forms.AdministratorUi.Views
{
    public partial class MaintenanceManagementWindow : PleasantWindow
    {
        public MaintenanceManagementWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 