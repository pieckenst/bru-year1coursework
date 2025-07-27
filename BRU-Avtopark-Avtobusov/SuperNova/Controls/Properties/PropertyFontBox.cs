using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using SuperNova.Runtime.BuiltinTypes;
using SuperNova.Utils;
using Classic.CommonControls.Dialogs;

namespace SuperNova.Controls;

public class PropertyFontBox : TemplatedControl
{
    public static readonly StyledProperty<VBFont> FontProperty = AvaloniaProperty.Register<PropertyFontBox, VBFont>("Font", defaultBindingMode: BindingMode.TwoWay, defaultValue: VBFont.Default);
    public static readonly DirectProperty<PropertyFontBox, string?> FontNameProperty = AvaloniaProperty.RegisterDirect<PropertyFontBox, string?>("FontName", o => o.FontName);

    public string? FontName
    {
        get => Font.FontFamily.Name;
    }

    public VBFont Font
    {
        get => GetValue(FontProperty);
        set => SetValue(FontProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var openFontWindowButton = e.NameScope.Get<Button>("PART_OpenFontWindowButton");
        openFontWindowButton.Click += OnButtonClick;
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        
    }

    static PropertyFontBox()
    {
        FontProperty.Changed.AddClassHandler<PropertyFontBox>((box, e) =>
        {
            box.RaisePropertyChanged(FontNameProperty, null, box.FontName);
        });
    }
}