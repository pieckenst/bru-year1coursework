using AvaloniaEdit.Document;
using SuperNova.Utils;
using Dock.Model.Mvvm.Controls;

namespace SuperNova.Tools;

public partial class ImmediateToolViewModel : EditorToolBase
{
    public TextDocument Document { get; } = new();

    public ImmediateToolViewModel()
    {
        Title = "Immediate";
        CanPin = false;
        CanClose = true;
    }
}