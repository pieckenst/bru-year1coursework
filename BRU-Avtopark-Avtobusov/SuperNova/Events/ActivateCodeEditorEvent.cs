using SuperNova.IDE;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.Events;

public class ActivateCodeEditorEvent : IEvent
{
    public ActivateCodeEditorEvent(FormDefinition form)
    {
        Form = form;
    }

    public FormDefinition Form { get; }
    public bool Handled { get; set; }
}