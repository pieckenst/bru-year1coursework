using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SuperNova.Forms.ViewModels;

namespace SuperNova.Forms.Views;

public partial class NewProjectView : UserControl
{
    public NewProjectView()
    {
        InitializeComponent();
    }

    private void TemplateDoubleTap(object? sender, TappedEventArgs e)
    {
        if (DataContext is NewProjectViewModel vm)
        {
            if (vm.Ok.CanExecute(null))
                vm.Ok.Execute(null);
        }
    }
}