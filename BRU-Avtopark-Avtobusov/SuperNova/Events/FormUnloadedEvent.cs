using SuperNova.IDE;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.Events;

public class FormUnloadedEvent : IEvent
{
    public FormDefinition Form { get; }

    public FormUnloadedEvent(FormDefinition form)
    {
        Form = form;
    }
}