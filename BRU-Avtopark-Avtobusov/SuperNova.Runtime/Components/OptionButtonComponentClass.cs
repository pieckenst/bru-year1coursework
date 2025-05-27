using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SuperNova.Runtime.BuiltinControls;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public class OptionButtonComponentClass : ComponentBaseClass
{
    public OptionButtonComponentClass() : base([BackColorProperty,
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

    public override string Name => "Option";
    public override string VBTypeName => "VB.OptionButton";

    protected override Control InstantiateInternal(ComponentInstance instance)
    {
        return new VBOptionButton()
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
    }

    public static ComponentBaseClass Instance { get; } = new OptionButtonComponentClass();
}