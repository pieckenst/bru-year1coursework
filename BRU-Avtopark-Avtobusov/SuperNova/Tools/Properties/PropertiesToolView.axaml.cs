using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SuperNova.Tools.Properties;

public partial class PropertiesToolView : UserControl
{
    private ComboBox? categoryComboBox;
    private ItemsControl? propertiesItemsControl;

    public PropertiesToolView()
    {
        InitializeComponent();
        InitializeControls();
        SetupEventHandlers();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls()
    {
        categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
        propertiesItemsControl = this.FindControl<ItemsControl>("PropertiesItemsControl");
    }

    private void SetupEventHandlers()
    {
        if (categoryComboBox != null)
            categoryComboBox.SelectionChanged += CategoryComboBox_SelectionChanged;
    }

    private void CategoryComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is PropertiesToolViewModel vm && 
            e.AddedItems.Count > 0 && 
            e.AddedItems[0] is ComboBoxItem selectedItem)
        {
            vm.UpdateProperties(selectedItem.Content?.ToString() ?? string.Empty);
        }
    }
} 