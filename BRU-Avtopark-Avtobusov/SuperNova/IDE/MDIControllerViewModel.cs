using SuperNova.Forms.ViewModels;
using SuperNova.Utils;
using Dock.Model.Mvvm.Controls;

namespace SuperNova.IDE;

public class MDIControllerViewModel : Document
{
    public IMdiWindowManager MdiWindowManager { get; }
    public IEventBus EventBus { get; }

    public DelegateCommand<BaseEditorWindowViewModel> CloseWindowCommand { get; }

    public MDIControllerViewModel(IMdiWindowManager mdiWindowManager,
        IEventBus eventBus)
    {
        MdiWindowManager = mdiWindowManager;
        EventBus = eventBus;
        CloseWindowCommand = new DelegateCommand<BaseEditorWindowViewModel>(window => MdiWindowManager.CloseWindow(window), _ => true);
    }
}