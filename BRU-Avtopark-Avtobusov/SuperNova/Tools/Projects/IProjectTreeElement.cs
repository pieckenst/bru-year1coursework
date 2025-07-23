using System.ComponentModel;

namespace SuperNova.Tools;

public interface IProjectTreeElement : INotifyPropertyChanged
{
    public bool IsExpanded { get; set; }
}