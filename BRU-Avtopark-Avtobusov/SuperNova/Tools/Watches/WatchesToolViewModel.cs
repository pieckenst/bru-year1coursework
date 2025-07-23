using Dock.Model.Mvvm.Controls;

namespace SuperNova.Tools;

public class WatchesToolViewModel : Tool
{
    public WatchesToolViewModel()
    {
        Title = "Watches";
        CanPin = false;
        CanClose = true;
    }
}