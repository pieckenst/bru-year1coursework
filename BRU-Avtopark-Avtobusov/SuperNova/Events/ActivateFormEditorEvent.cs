using SuperNova.IDE;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.Events;

public class ActivateFormEditorEvent : IEvent
{
    public ActivateFormEditorEvent(FormDefinition form)
    {
        Form = form;
    }

    public FormDefinition Form { get; }
    public bool Handled { get; set; }
}