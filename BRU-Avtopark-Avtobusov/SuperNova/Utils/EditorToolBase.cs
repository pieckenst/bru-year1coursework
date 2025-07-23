using Dock.Model.Mvvm.Controls;
using PropertyChanged.SourceGenerator;

namespace SuperNova.Utils;

public partial class EditorToolBase : Tool
{
    [Notify] private bool isActive;
}