using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SuperNova.Runtime.BuiltinControls;
using SuperNova.Runtime.BuiltinTypes;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public class CheckBoxComponentClass : ComponentBaseClass
{
    public CheckBoxComponentClass() : base([BackColorProperty,
        CaptionProperty,
        EnabledProperty,
        FontProperty,
        ForeColorProperty,
        MousePointerProperty,
        RightToLeftProperty,
        ToolTipTextProperty,
        CheckValueProperty,
        AppearanceProperty,
        TabStopProperty,
        TabIndexProperty], [ClickEvent])
    {
    }

    public override string Name => "Check";
    public override string VBTypeName => "VB.CheckBox";

    protected override Control InstantiateInternal(ComponentInstance instance)
    {
        var checkBox = new VBCheckBox()
        {
            Content = instance.GetPropertyOrDefault(CaptionProperty),
            Appearance = instance.GetPropertyOrDefault(AppearanceProperty),
            Value = instance.GetPropertyOrDefault(CheckValueProperty),
            [AttachedProperties.BackColorProperty] = instance.GetPropertyOrDefault(BackColorProperty),
            [AttachedProperties.ForeColorProperty] = instance.GetPropertyOrDefault(ForeColorProperty),
            [AttachedProperties.FontProperty] = instance.GetPropertyOrDefault(FontProperty),
            Cursor = new Cursor(instance.GetPropertyOrDefault(MousePointerProperty)),
            FlowDirection = instance.GetPropertyOrDefault(RightToLeftProperty) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
        };
        return checkBox;
    }

    static CheckBoxComponentClass()
    {
        BackColorProperty.OverrideDefault<CheckBoxComponentClass>(VBColor.FromSystemColor(VbSystemColor.Btnface));
    }

    public static ComponentBaseClass Instance { get; } = new CheckBoxComponentClass();
}