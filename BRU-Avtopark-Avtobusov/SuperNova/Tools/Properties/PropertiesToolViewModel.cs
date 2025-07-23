using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Dock.Model.Mvvm.Controls;
using SuperNova.IDE;
using PropertyChanged.SourceGenerator;
using R3;
using Avalonia.Media;
using Material.Icons;

namespace SuperNova.Tools.Properties;

public partial class PropertyItem
{
    private string v;
    private Control control;

    public PropertyItem(string v, Control control)
    {
        this.v = v;
        this.control = control;
    }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Control Value { get; set; } = new TextBox();
}

public partial class PropertiesToolViewModel : Tool
{
    private readonly IMdiWindowManager mdiWindowManager;
    private readonly IWindowManager windowManager;

    [Notify] private ObservableCollection<string> categories = new() 
    { 
        "Основные",
        "Внешний вид",
        "Данные",
        "События"
    };
    
    [Notify] private string? selectedCategory;
    [Notify] private ObservableCollection<PropertyItem> properties = new();
    [Notify] private object? selectedObject;

    public PropertiesToolViewModel(IMdiWindowManager mdiWindowManager, IWindowManager windowManager)
    {
        this.mdiWindowManager = mdiWindowManager;
        this.windowManager = windowManager;
        
        Title = "Свойства";
        CanPin = true;
        CanClose = true;

        // Subscribe to active window changes to update properties
        mdiWindowManager.ObservePropertyChanged(x => x.ActiveWindow)
            .Subscribe(window =>
            {
                if (window != null)
                {
                    SelectedObject = window;
                    UpdateProperties("Основные");
                }
                else
                {
                    SelectedObject = null;
                    properties.Clear();
                }
            });

        // Subscribe to category changes
        this.ObservePropertyChanged(x => x.SelectedCategory)
            .Subscribe(category =>
            {
                if (category != null)
                {
                    UpdateProperties(category);
                }
            });

        // Set initial category
        SelectedCategory = "Основные";
    }

    public void UpdateProperties(string category)
    {
        properties.Clear();

        if (SelectedObject == null)
            return;

        switch (category)
        {
            case "Основные":
                AddBasicProperties();
                break;
            case "Внешний вид":
                AddAppearanceProperties();
                break;
            case "Данные":
                AddDataProperties();
                break;
            case "События":
                AddEventProperties();
                break;
        }
    }

    private void AddBasicProperties()
    {
        properties.Add(new PropertyItem("Название", CreateTextBox("property-textbox"))
        { 
            Description = "Отображаемое имя объекта"
        });
        
        properties.Add(new PropertyItem("Описание", CreateTextBox("property-textbox"))
        { 
            Description = "Краткое описание назначения объекта"
        });
        
        properties.Add(new PropertyItem("Тип", CreateComboBox("property-combobox"))
        { 
            Description = "Тип объекта в системе"
        });
        
        properties.Add(new PropertyItem("Статус", CreateComboBox("property-combobox"))
        { 
            Description = "Текущее состояние объекта"
        });
    }

    private void AddAppearanceProperties()
    {
        properties.Add(new PropertyItem("Цвет фона", CreateColorPicker())
        { 
            Description = "Цвет заливки фона объекта"
        });
        
        properties.Add(new PropertyItem("Цвет текста", CreateColorPicker())
        { 
            Description = "Цвет отображаемого текста"
        });
        
        properties.Add(new PropertyItem("Шрифт", CreateFontPicker())
        { 
            Description = "Семейство шрифта для текста"
        });
        
        properties.Add(new PropertyItem("Размер", CreateNumericUpDown())
        { 
            Description = "Размер шрифта в пунктах"
        });
    }

    private void AddDataProperties()
    {
        properties.Add(new PropertyItem(
            "Источник данных",
            CreateComboBox("property-combobox")
        ) { 
            Description = "Источник данных для объекта"
        });
        
        properties.Add(new PropertyItem(
            "Фильтр",
            CreateTextBox("property-textbox")
        ) { 
            Description = "Условия фильтрации данных"
        });
        
        properties.Add(new PropertyItem(
            "Сортировка",
            CreateComboBox("property-combobox")
        ) { 
            Description = "Порядок сортировки данных"
        });
    }
    private void AddEventProperties()
    {
        properties.Add(new PropertyItem(
            "При загрузке",
            CreateEventEditor()
        ) { 
            Description = "Действия при загрузке объекта"
        });
        
        properties.Add(new PropertyItem(
            "При изменении",
            CreateEventEditor()
        ) { 
            Description = "Действия при изменении значений"
        });
        
        properties.Add(new PropertyItem(
            "При сохранении",
            CreateEventEditor()
        ) { 
            Description = "Действия при сохранении объекта"
        });
    }
    private Control CreateTextBox(string className)
    {
        return new TextBox { Classes = { className } };
    }

    private Control CreateComboBox(string className)
    {
        return new ComboBox { Classes = { className } };
    }

    private Control CreateColorPicker()
    {
        return new ColorPicker { Classes = { "property-combobox" } };
    }

    private Control CreateFontPicker()
    {
        return new ComboBox { Classes = { "property-combobox" } };
    }

    private Control CreateNumericUpDown()
    {
        return new NumericUpDown { Classes = { "property-textbox" } };
    }

    private Control CreateEventEditor()
    {
        var button = new Button { 
            Content = "...", 
            Classes = { "property-button" }
        };
        button.Click += (s, e) => OpenEventEditor();
        return button;
    }

    private void OpenEventEditor()
    {
        windowManager.MessageBox("Редактор событий будет доступен в следующей версии.", "В разработке");
    }
}

