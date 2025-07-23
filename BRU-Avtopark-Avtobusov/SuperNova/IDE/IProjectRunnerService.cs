using System.ComponentModel;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.IDE;

public interface IProjectRunnerService : INotifyPropertyChanged
{
    void RunProject(ProjectDefinition projectDefinition);
    void RunStartupProject();
    void BreakCurrentProject();
    void EndProject();
    void RestartProject();
    bool CanStartDefaultProject { get; }
    bool CanStartDefaultProjectWithFullCompile { get; }
    bool CanBreakProject { get; }
    bool CanEndProject { get; }
    bool CanRestartProject { get; }
    bool IsRunning { get; }
}