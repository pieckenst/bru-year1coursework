using Avalonia.Controls;
using Avalonia.Input;
using SuperNova.Runtime.BuiltinControls;
using SuperNova.Runtime.BuiltinTypes;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public class CommandButtonComponentClass : ComponentBaseClass
{
    public CommandButtonComponentClass() : base([CaptionProperty,
        BackColorProperty,
        ForeColorProperty,
        AppearanceProperty,
        FontProperty,
        MousePointerProperty,
        EnabledProperty,
        TabStopProperty,
        TabIndexProperty], [ClickEvent, GotFocusEvent, LostFocusEvent])
    {
    }

    public override string Name => "Command";
    public override string VBTypeName => "VB.CommandButton";

    protected override Control InstantiateInternal(ComponentInstance instance)
    {
        return new VBCommandButton()
        {
            Content = instance.GetPropertyOrDefault(CaptionProperty),
            [AttachedProperties.BackColorProperty] = instance.GetPropertyOrDefault(BackColorProperty),
            [AttachedProperties.ForeColorProperty] = instance.GetPropertyOrDefault(ForeColorProperty),
            [AttachedProperties.FontProperty] = instance.GetPropertyOrDefault(FontProperty),
            Cursor = new Cursor(instance.GetPropertyOrDefault(MousePointerProperty)),
        };
    }

    public static CommandButtonComponentClass Instance { get; } = new();
}