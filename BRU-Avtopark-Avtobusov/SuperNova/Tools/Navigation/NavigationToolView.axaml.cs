using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperNova.Tools.Navigation;

public partial class NavigationToolView : UserControl
{
    private TextBox? searchBox;
    private TreeView? navigationTree;

    public NavigationToolView()
    {
        InitializeComponent();
        InitializeControls();
        SetupEventHandlers();
        this.AttachedToVisualTree += OnAttachedToVisualTreeHandler;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls()
    {
        searchBox = this.FindControl<TextBox>("SearchBox");
        navigationTree = this.FindControl<TreeView>("NavigationTree");
    }

    private void SetupEventHandlers()
    {
        if (searchBox != null)
            searchBox.TextChanged += SearchBox_TextChanged;

        if (navigationTree != null)
            navigationTree.SelectionChanged += NavigationTree_SelectionChanged;
    }

    private void OnAttachedToVisualTreeHandler(object? sender, VisualTreeAttachmentEventArgs e)
    {
        
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (searchBox != null && !string.IsNullOrWhiteSpace(searchBox.Text))
        {
            // Removed filtering logic, handled by ViewModel
        }
    }

    private void NavigationTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is NavigationToolViewModel vm &&
            e.AddedItems.Count > 0 &&
            e.AddedItems[0] is TreeViewItem selectedItem)
        {
            vm.NavigateToItem(selectedItem.Header?.ToString() ?? string.Empty);
        }
    }

    private bool FilterTreeViewItems(IEnumerable<TreeViewItem>? items, string searchText)
    {
        if (items == null) return false;
        bool foundMatch = false;

        foreach (var treeItem in items)
        {
            bool headerMatch = treeItem.Header?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false;
            bool childrenMatch = FilterTreeViewItems(treeItem.Items.Cast<TreeViewItem>(), searchText);

            treeItem.IsVisible = headerMatch || childrenMatch;

            if (headerMatch || childrenMatch)
            {
                foundMatch = true;
                ExpandParents(treeItem);
            }
        }

        return foundMatch;
    }

    private void ResetTreeViewItemsVisibility(IEnumerable<TreeViewItem>? items)
    {
        if (items == null) return;

        foreach (var treeItem in items)
        {
            treeItem.IsVisible = true;
            ResetTreeViewItemsVisibility(treeItem.Items.Cast<TreeViewItem>());
        }
    }

    private void ExpandParents(TreeViewItem item)
    {
        var parent = item.Parent as TreeViewItem;
        while (parent != null)
        {
            parent.IsExpanded = true;
            parent = parent.Parent as TreeViewItem;
        }
    }
}