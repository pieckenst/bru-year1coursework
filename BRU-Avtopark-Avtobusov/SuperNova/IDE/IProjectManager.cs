using System;
using System.Collections.Generic;
using System.ComponentModel;
using SuperNova.Projects;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.IDE;

public interface IProjectManager : INotifyPropertyChanged
{
    public IReadOnlyList<ProjectDefinition> LoadedProjects { get; }
    public event Action<ProjectDefinition>? ProjectLoaded;
    public event Action<ProjectDefinition>? ProjectUnloaded;

    public ProjectDefinition? StartupProject { get; set; }

    public ProjectDefinition NewProject(IProjectTemplate projectTemplate, string name);
    void AddProject(ProjectDefinition project);
    void UnloadAllProjects();
    void UnloadProject(ProjectDefinition projectManager);
}