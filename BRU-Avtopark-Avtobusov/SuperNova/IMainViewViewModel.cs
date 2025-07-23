using Dock.Model.Controls;
using SuperNova.IDE;
using SuperNova.Projects;
using Dock.Model.Core;

namespace SuperNova.Forms.ViewModels;

public interface IMainViewViewModel
{
    IMdiWindowManager MdiWindowManager { get; }
    bool IsStandardToolbarVisible { get; set; }
    IRootDock Layout { get; }
    IFocusedProjectUtil FocusedProjectUtil { get; }
} 