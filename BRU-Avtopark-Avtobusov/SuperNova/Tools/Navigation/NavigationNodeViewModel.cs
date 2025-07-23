using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;


namespace SuperNova.Tools.Navigation;

public enum NavigationNodeType
{
    Folder,
    Document,
    Report,
    Reference,
    Action
}

public partial class NavigationNodeViewModel : ObservableObject
{
    [ObservableProperty] private string header = string.Empty;
    [ObservableProperty] private string? icon;
    [ObservableProperty] private NavigationNodeType nodeType;
    [ObservableProperty] private bool isExpanded;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private ObservableCollection<NavigationNodeViewModel> children = new();
    [ObservableProperty] private ICommand? command;
    [ObservableProperty] private object? tag;

    public NavigationNodeViewModel(string header, NavigationNodeType nodeType, string? icon = null, ICommand? command = null)
    {
        this.header = header;
        this.nodeType = nodeType;
        this.icon = icon;
        this.command = command;
    }

    public NavigationNodeViewModel CloneWithChildren()
    {
        var clone = new NavigationNodeViewModel(Header, NodeType, Icon, Command)
        {
            Tag = Tag
        };
        foreach (var child in Children)
            clone.Children.Add(child.CloneWithChildren());
        return clone;
    }
}
