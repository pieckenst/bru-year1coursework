using Avalonia.Controls;
using SuperNova.Runtime.BuiltinControls;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public class TimerComponentClass : ComponentBaseClass
{
    public TimerComponentClass() : base([EnabledProperty, IntervalProperty], [TimerEvent])
    {
    }

    public override string Name => "Timer";
    public override string VBTypeName => "VB.Timer";

    protected override Control InstantiateInternal(ComponentInstance instance)
    {
        return new VBTimer()
        {
            Interval = instance[IntervalProperty]
        };
    }

    public static EventClass TimerEvent = new EventClass("Timer");

    public static ComponentBaseClass Instance { get; } = new TimerComponentClass();
}