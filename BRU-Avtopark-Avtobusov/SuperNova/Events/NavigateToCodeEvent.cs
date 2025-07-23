using SuperNova.IDE;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.Events;

public class CreateOrNavigateToSubEvent : IEvent
{
    public CreateOrNavigateToSubEvent(FormDefinition form, string sub)
    {
        Form = form;
        Sub = sub;
    }

    public FormDefinition Form { get; }
    public string Sub { get; }
}