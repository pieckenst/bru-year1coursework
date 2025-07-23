using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SuperNova.Runtime.ProjectElements;

namespace SuperNova.Projects;

public class ProjectTemplate : IProjectTemplate
{
    private readonly Func<string, ProjectDefinition> creator;

    public ProjectTemplate(string name,
        string iconName,
        bool supported,
        Func<string, ProjectDefinition> creator)
    {
        this.creator = creator;
        Supported = supported;
        Name = name;
        Icon = new Bitmap(AssetLoader.Open(new Uri($"avares://SuperNova/Icons/types/{iconName}.gif")));
    }

    public string Name { get; set; }

    public Bitmap Icon { get; set; }

    public bool Supported { get; set; }

    public ProjectDefinition Create(string name)
    {
        return creator(name);
    }
}