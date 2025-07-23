using SuperNova.IDE;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.Events;

public class ProjectUnloadedEvent : IEvent
{
    public ProjectDefinition Project { get; }

    public ProjectUnloadedEvent(ProjectDefinition project)
    {
        Project = project;
    }
}