using Avalonia;
using Avalonia.Controls;
using SuperNova.Runtime.BuiltinControls;
using SuperNova.Runtime.BuiltinTypes;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public class FormComponentClass : ComponentBaseClass
{
    public FormComponentClass() : base([CaptionProperty,
    BackColorProperty,
    ForeColorProperty,
    ShowInTaskbarProperty,
    FontProperty,
    StartUpPositionProperty], [ClickEvent])
    {
    }

    public override string Name => "Form";
    public override string VBTypeName => "VB.Form";

    protected override Control InstantiateInternal(ComponentInstance instance)
    {
        return new Control()
        {
        };
    }

    public VBFormRuntime InstantiateWindow(ComponentInstance instance)
    {
        var form = new VBFormRuntime()
        {
            Title = instance.GetPropertyOrDefault(CaptionProperty),
            Width = instance.GetPropertyOrDefault(WidthProperty),
            Height = instance.GetPropertyOrDefault(HeightProperty),
            [AttachedProperties.BackColorProperty] = instance.GetPropertyOrDefault(BackColorProperty),
            [AttachedProperties.ForeColorProperty] = instance.GetPropertyOrDefault(ForeColorProperty),
            [AttachedProperties.FontProperty] = instance.GetPropertyOrDefault(FontProperty),
            WindowStartupLocation = instance.GetPropertyOrDefault(StartUpPositionProperty).ToAvalonia(),
            ShowInTaskbar = instance.GetPropertyOrDefault(ShowInTaskbarProperty),
            Position = new PixelPoint((int)instance.GetPropertyOrDefault(LeftProperty), (int)instance.GetPropertyOrDefault(TopProperty))
        };
        VBProps.SetName(form, instance.GetPropertyOrDefault(NameProperty));
        return form;
    }

    public static FormComponentClass Instance { get; } = new();
}