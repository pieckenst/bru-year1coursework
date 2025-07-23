using Avalonia.Media.Imaging;
using SuperNova.Projects;
using SuperNova.Runtime.ProjectElements;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperNova.Forms.ViewModels;

public class ProjectTemplateViewModel : ObservableObject
{
    private readonly IProjectTemplate template;

    public string Name => template.Name;

    public Bitmap Icon => template.Icon;

    public bool Supported => template.Supported;

    public IProjectTemplate? Template => template;

    public ProjectTemplateViewModel(IProjectTemplate template)
    {
        this.template = template;
    }
}