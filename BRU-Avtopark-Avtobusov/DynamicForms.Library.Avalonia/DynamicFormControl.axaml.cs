using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicForms.Library.Avalonia.Groups;
using DynamicForms.Library.Core;
using DynamicForms.Library.Core.Attributes;

namespace DynamicForms.Library.Avalonia;

public partial class DynamicFormControl : UserControl
{
    private bool _loaded;
    
    public DynamicFormControl()
    {
        InitializeComponent();
    }
    
    public static readonly StyledProperty<object?> DataProperty = AvaloniaProperty.Register<DynamicFormControl, object?>(
        nameof(Data));

    public object? Data
    {
        get => GetValue(DataProperty);
        set
        {
            SetValue(DataProperty, value);
            LoadDataObject();
        }
    }
    
    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_loaded)
        {
            return;
        }
        
        LoadDataObject();
        _loaded = true;
    }

    private void LoadDataObject()
    {
        if (Data == null)
        {
            return;
        }
        var dynamicForm = new DynamicForm(Data);

        var dockPanel = this.Find<DockPanel>(nameof(ParentPanel))!;

        var mainGroupControl = CreateFormGroup(dynamicForm.ParentGroup.GroupName, dynamicForm.ParentGroup.Style,
            dynamicForm.ParentGroup.Type,
            dynamicForm.ParentGroup.Objects,
            dynamicForm.ParentGroup.Attributes);

        dockPanel.Children.Add(mainGroupControl);
    }

    private Control CreateFormGroup(string groupName, DynamicFormGroupStyle style, DynamicFormLayout type, List<DynamicFormObject> groupObjects, DynamicFormGroupAttribute? attributes)
    {
        DynamicFormGroupStyleControl groupStyleControl = style switch
        {
            DynamicFormGroupStyle.Basic => new DynamicFormGroupStyleBasic(),
            DynamicFormGroupStyle.GroupBox => new DynamicFormGroupStyleGroupBox(groupName),
            DynamicFormGroupStyle.Expander => new DynamicFormGroupStyleExpander(groupName, (attributes as DynamicFormGroupExpanderAttribute)?.IsExpanded ?? false),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
            
        DynamicFormGroupLayoutControl groupLayoutControl = type switch
        {
            DynamicFormLayout.Vertical => new DynamicFormGroupLayoutVertical(),
            DynamicFormLayout.TwoColumns => new DynamicFormGroupLayoutTwoColumn(),
            DynamicFormLayout.SideBySide => new DynamicFormGroupLayoutSideBySide(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        var lastGroup = groupObjects.LastOrDefault(x => x is DynamicFormGroup);

        foreach (var formObject in groupObjects)
        {
            if (formObject is DynamicFormField field)
            {
                groupLayoutControl.AddField(field);
            }
            else if (formObject is DynamicFormGroup group)
            {
                var subGroupControl = CreateFormGroup(group.GroupName, group.Style, group.Type, group.Objects, group.Attributes);
                if (formObject != lastGroup)
                {
                    subGroupControl.Margin = new Thickness(0, 0, 0, 5);
                }

                if (!string.IsNullOrEmpty(group.Attributes?.VisibleWhenTrue) && group.Value is INotifyPropertyChanged notifyParent)
                {
                    var property = group.Value.GetType().GetProperty(group.Attributes.VisibleWhenTrue);
                    subGroupControl.IsVisible = (bool?)property?.GetValue(group.Value) != false;

                    notifyParent.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName != group.Attributes.VisibleWhenTrue)
                        {
                            return;
                        }

                        if (Dispatcher.UIThread.CheckAccess())
                        {
                            subGroupControl.IsVisible = (bool?)property?.GetValue(group.Value) != false;    
                        }
                        else
                        {
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                subGroupControl.IsVisible = (bool?)property?.GetValue(group.Value) != false;
                            });
                        }
                    };
                }
                
                groupLayoutControl.AddControl(subGroupControl);
            }
            else
            {
                throw new InvalidOperationException($"Unknown object type {formObject.GetType().Name}");
            }
        }
        
        groupStyleControl.AddBody(groupLayoutControl);

        return groupStyleControl;
    }

    public static readonly StyledProperty<string> TemplatePathProperty = 
        AvaloniaProperty.Register<DynamicFormControl, string>(nameof(TemplatePath));

    public string TemplatePath
    {
        get => GetValue(TemplatePathProperty);
        set
        {
            SetValue(TemplatePathProperty, value);
            LoadFromTemplate();
        }
    }

    private void LoadFromTemplate()
    {
        if (Data == null || string.IsNullOrEmpty(TemplatePath))
            return;

        try
        {
            var form = DynamicForm.LoadFromFile(TemplatePath, Data);
            var dockPanel = this.Find<DockPanel>(nameof(ParentPanel))!;
            dockPanel.Children.Clear();

            var mainGroupControl = CreateFormGroup(form.ParentGroup.GroupName, 
                form.ParentGroup.Style,
                form.ParentGroup.Type,
                form.ParentGroup.Objects,
                form.ParentGroup.Attributes);

            dockPanel.Children.Add(mainGroupControl);
        }
        catch (Exception ex)
        {
            // Handle template loading errors
            System.Diagnostics.Debug.WriteLine($"Error loading template: {ex}");
        }
    }

    public void SaveTemplate(string filePath)
    {
        if (Data == null)
            return;

        var form = new DynamicForm(Data);
        form.SaveToFile(filePath);
    }
}