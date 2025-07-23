using Dock.Model.Mvvm.Controls;

namespace SuperNova.Tools;

public class FormLayoutToolViewModel : Tool
{
    public FormLayoutToolViewModel()
    {
        Title = "Form Layout";
        CanPin = false;
        CanClose = true;
    }
}