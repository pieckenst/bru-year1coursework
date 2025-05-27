using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SuperNova.Runtime.BuiltinControls;
using SuperNova.Runtime.BuiltinTypes;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public class ListBoxComponentClass : ComponentBaseClass
{
    public ListBoxComponentClass() : base([EnabledProperty,
        FontProperty,
        ForeColorProperty,
        BackColorProperty,
        ListProperty,
        LockedProperty,
        MousePointerProperty,
        RightToLeftProperty,
        AppearanceProperty,
        TabStopProperty,
        TabIndexProperty], [ClickEvent])
    {
    }

    public override string Name => "List";
    public override string VBTypeName => "VB.ListBox";

    protected override Control InstantiateInternal(ComponentInstance instance)
    {
        return new VBListBox()
        {
            ItemsSource = instance.GetPropertyOrDefault(ListProperty),
            [AttachedProperties.BackColorProperty] = instance.GetPropertyOrDefault(BackColorProperty),
            [AttachedProperties.ForeColorProperty] = instance.GetPropertyOrDefault(ForeColorProperty),
            [AttachedProperties.FontProperty] = instance.GetPropertyOrDefault(FontProperty),
            Cursor = new Cursor(instance.GetPropertyOrDefault(MousePointerProperty)),
            FlowDirection = instance.GetPropertyOrDefault(RightToLeftProperty) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
        };
    }

    static ListBoxComponentClass()
    {
        BackColorProperty.OverrideDefault<ListBoxComponentClass>(VBColor.FromSystemColor(VbSystemColor.Window));
    }

    public static ComponentBaseClass Instance { get; } = new ListBoxComponentClass();
}