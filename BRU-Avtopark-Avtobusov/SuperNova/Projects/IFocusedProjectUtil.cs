using System.ComponentModel;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.Projects;

public interface IFocusedProjectUtil : INotifyPropertyChanged
{
    public ProjectDefinition? FocusedProject { get; }
    public ProjectDefinition? FocusedOrStartupProject { get; }
    public FormDefinition? FocusedForm { get; }
    public string FocusedComponentPosition { get; }
    public string FocusedComponentSize { get; }
}