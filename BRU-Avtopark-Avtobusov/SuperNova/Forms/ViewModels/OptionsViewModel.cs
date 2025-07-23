using System;
using SuperNova.IDE;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuperNova.Forms.ViewModels;

public class OptionsViewModel : ObservableObject, IDialog
{
    public string Title => "Options";
    public bool CanResize => false;
    public event Action<bool>? CloseRequested;

    public void Close()
    {
        CloseRequested?.Invoke(false);
    }
}