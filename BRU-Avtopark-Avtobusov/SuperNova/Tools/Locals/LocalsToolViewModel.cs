using Dock.Model.Mvvm.Controls;

namespace SuperNova.Tools;

public class LocalsToolViewModel : Tool
{
    public LocalsToolViewModel()
    {
        Title = "Locals";
        CanPin = false;
        CanClose = true;
    }
}