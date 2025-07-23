using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SuperNova.IDE;
using PropertyChanged.SourceGenerator;
using Avalonia.Controls;
using Material.Icons;
using R3;

namespace SuperNova.Tools.Navigation;

public partial class NavigationToolViewModel : Tool
{
    private readonly IMdiWindowManager mdiWindowManager;
    private readonly IWindowManager windowManager;

    [ObservableProperty]
private string searchText = string.Empty;
partial void OnSearchTextChanged(string value)
{
    UpdateFilteredNavigationNodes();
}
    [Notify] private NavigationNodeViewModel? selectedNode;
    
    public ObservableCollection<NavigationNodeViewModel> NavigationNodes { get; } = new();
    public ObservableCollection<NavigationNodeViewModel> FilteredNavigationNodes { get; } = new();

    public ICommand OpenCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand DeleteCTXCommand { get; }

    [Notify] private string? selectedCategory;
   
    [Notify] private object? selectedObject;

    public NavigationToolViewModel(IMdiWindowManager mdiWindowManager, IWindowManager windowManager)
    {
        this.mdiWindowManager = mdiWindowManager;
        this.windowManager = windowManager;
        
        Title = "Навигация";
        CanPin = true;
        CanClose = true;
        CanFloat = true;

        OpenCommand = new RelayCommand<NavigationNodeViewModel?>(OnOpen);
        AddCommand = new RelayCommand<NavigationNodeViewModel?>(OnAdd);
        DeleteCTXCommand = new RelayCommand<NavigationNodeViewModel?>(OnDelete);

        

        InitializeNavigationItems();
    }

    private void InitializeNavigationItems()
    {
        NavigationNodes.Clear();
        var salesNode = new NavigationNodeViewModel("Продажи", NavigationNodeType.Folder)
        {
            Children = new ObservableCollection<NavigationNodeViewModel>
            {
                new NavigationNodeViewModel("Продажа билетов", NavigationNodeType.Document),
                new NavigationNodeViewModel("Возврат билетов", NavigationNodeType.Document),
                new NavigationNodeViewModel("История продаж", NavigationNodeType.Document),
                new NavigationNodeViewModel("Отчеты по продажам", NavigationNodeType.Report)
            }
        };
        NavigationNodes.Add(salesNode);

        var routesNode = new NavigationNodeViewModel("Маршруты", NavigationNodeType.Folder)
        {
            Children = new ObservableCollection<NavigationNodeViewModel>
            {
                new NavigationNodeViewModel("Управление маршрутами", NavigationNodeType.Document),
                new NavigationNodeViewModel("Расписание", NavigationNodeType.Document),
                new NavigationNodeViewModel("Тарифы", NavigationNodeType.Reference)
            }
        };
        NavigationNodes.Add(routesNode);

        var busNode = new NavigationNodeViewModel("Автопарк", NavigationNodeType.Folder)
        {
            Children = new ObservableCollection<NavigationNodeViewModel>
            {
                new NavigationNodeViewModel("Автобусы", NavigationNodeType.Document),
                new NavigationNodeViewModel("Техобслуживание", NavigationNodeType.Document),
                new NavigationNodeViewModel("Ремонты", NavigationNodeType.Document)
            }
        };
        NavigationNodes.Add(busNode);

        var staffNode = new NavigationNodeViewModel("Персонал", NavigationNodeType.Folder)
        {
            Children = new ObservableCollection<NavigationNodeViewModel>
            {
                new NavigationNodeViewModel("Сотрудники", NavigationNodeType.Document),
                new NavigationNodeViewModel("График работы", NavigationNodeType.Document),
                new NavigationNodeViewModel("Зарплата", NavigationNodeType.Document)
            }
        };
        NavigationNodes.Add(staffNode);

        var reportsNode = new NavigationNodeViewModel("Отчеты", NavigationNodeType.Folder)
        {
            Children = new ObservableCollection<NavigationNodeViewModel>
            {
                new NavigationNodeViewModel("Финансовые отчеты", NavigationNodeType.Report),
                new NavigationNodeViewModel("Статистика", NavigationNodeType.Report),
                new NavigationNodeViewModel("Аналитика", NavigationNodeType.Report)
            }
        };
        NavigationNodes.Add(reportsNode);

        var adminNode = new NavigationNodeViewModel("Администрирование", NavigationNodeType.Folder)
        {
            Children = new ObservableCollection<NavigationNodeViewModel>
            {
                new NavigationNodeViewModel("Пользователи", NavigationNodeType.Document),
                new NavigationNodeViewModel("Роли и права", NavigationNodeType.Document),
                new NavigationNodeViewModel("Настройки системы", NavigationNodeType.Document),
                new NavigationNodeViewModel("Резервное копирование", NavigationNodeType.Action)
            }
        };
        NavigationNodes.Add(adminNode);
        UpdateFilteredNavigationNodes();
    }

    public void NavigateToItem(string path)
    {
        switch (path)
        {
            case "Продажа билетов":
                windowManager.MessageBox("Функция продажи билетов будет доступна в следующей версии.", "В разработке");
                break;
            case "Возврат билетов":
                windowManager.MessageBox("Функция возврата билетов будет доступна в следующей версии.", "В разработке");
                break;
            case "История продаж":
                windowManager.MessageBox("Функция истории продаж будет доступна в следующей версии.", "В разработке");
                break;
            case "Управление маршрутами":
                windowManager.MessageBox("Функция управления маршрутами будет доступна в следующей версии.", "В разработке");
                break;
            case "Расписание":
                windowManager.MessageBox("Функция расписания будет доступна в следующей версии.", "В разработке");
                break;
            case "Автобусы":
                windowManager.MessageBox("Функция управления автобусами будет доступна в следующей версии.", "В разработке");
                break;
            case "Техобслуживание":
                windowManager.MessageBox("Функция техобслуживания будет доступна в следующей версии.", "В разработке");
                break;
            case "Сотрудники":
                windowManager.MessageBox("Функция управления сотрудниками будет доступна в следующей версии.", "В разработке");
                break;
            case "График работы":
                windowManager.MessageBox("Функция графика работы будет доступна в следующей версии.", "В разработке");
                break;
            case "Зарплата":
                windowManager.MessageBox("Функция расчета зарплаты будет доступна в следующей версии.", "В разработке");
                break;
            case "Пользователи":
                windowManager.MessageBox("Функция управления пользователями будет доступна в следующей версии.", "В разработке");
                break;
            case "Роли и права":
                windowManager.MessageBox("Функция управления ролями будет доступна в следующей версии.", "В разработке");
                break;
            case "Настройки системы":
                windowManager.MessageBox("Функция настройки системы будет доступна в следующей версии.", "В разработке");
                break;
        }
    }

    private void UpdateFilteredNavigationNodes()
    {
        FilteredNavigationNodes.Clear();
        foreach (var node in NavigationNodes)
        {
            var filtered = FilterNodeRecursive(node, SearchText);
            if (filtered != null)
                FilteredNavigationNodes.Add(filtered);
        }
    }

    private NavigationNodeViewModel? FilterNodeRecursive(NavigationNodeViewModel node, string search)
    {
        if (string.IsNullOrWhiteSpace(search) || node.Header.Contains(search, StringComparison.OrdinalIgnoreCase))
            return node.CloneWithChildren();
        var matchedChildren = node.Children
            .Select(child => FilterNodeRecursive(child, search))
            .Where(child => child != null)
            .ToList();
        if (matchedChildren.Any())
        {
            var clone = node.CloneWithChildren();
            clone.Children.Clear();
            foreach (var child in matchedChildren)
                clone.Children.Add(child!);
            return clone;
        }
        return null;
    }

    private void OnOpen(NavigationNodeViewModel? node)
    {
        windowManager.MessageBox("Открытие элементов реализуется в будущих версиях.", "Stub");
    }

    private void OnAdd(NavigationNodeViewModel? node)
    {
        windowManager.MessageBox("Добавление элементов реализуется в будущих версиях.", "Stub");
    }

    private void OnDelete(NavigationNodeViewModel? node)
    {
        windowManager.MessageBox("Удаление элементов реализуется в будущих версиях.", "Stub");
    }
} 