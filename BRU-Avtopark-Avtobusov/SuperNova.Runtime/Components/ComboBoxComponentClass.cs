using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SuperNova.Runtime.BuiltinControls;
using SuperNova.Runtime.BuiltinTypes;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public class ComboBoxComponentClass : ComponentBaseClass
{
    public ComboBoxComponentClass() : base([EnabledProperty,
        FontProperty,
        ForeColorProperty,
        BackColorProperty,
        ListProperty,
        LockedProperty,
        MousePointerProperty,
        RightToLeftProperty,
        AppearanceProperty,
        TabStopProperty,
        TabIndexProperty])
    {
    }

    public override string Name => "Combo";
    public override string VBTypeName => "VB.ComboBox";

    protected override Control InstantiateInternal(ComponentInstance instance)
    {
        return new VBComboBox()
        {
            ItemsSource = instance.GetPropertyOrDefault(ListProperty),
            [AttachedProperties.BackColorProperty] = instance.GetPropertyOrDefault(BackColorProperty),
            [AttachedProperties.ForeColorProperty] = instance.GetPropertyOrDefault(ForeColorProperty),
            [AttachedProperties.FontProperty] = instance.GetPropertyOrDefault(FontProperty),
            Cursor = new Cursor(instance.GetPropertyOrDefault(MousePointerProperty)),
            FlowDirection = instance.GetPropertyOrDefault(RightToLeftProperty) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };
    }

    static ComboBoxComponentClass()
    {
        BackColorProperty.OverrideDefault<ComboBoxComponentClass>(VBColor.FromSystemColor(VbSystemColor.Window));
    }

    public static ComponentBaseClass Instance { get; } = new ComboBoxComponentClass();
}