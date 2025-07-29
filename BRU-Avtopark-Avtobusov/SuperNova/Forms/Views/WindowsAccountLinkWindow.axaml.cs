using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using PleasantUI.Controls;
using ReactiveUI;
using SuperNova.Forms.ViewModels;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace SuperNova.Forms.Views
{
    
    public partial class WindowsAccountLinkWindow : PleasantWindow 
    {
        public WindowsAccountLinkWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        
        var viewModel = new WindowsAccountLinkViewModel();
        viewModel.RequestClose += (s, e) => Close();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
} }
