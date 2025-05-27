// UI/Avalonia/Views/AuthWindow.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using Serilog;
using TicketSalesApp.UI.Avalonia.ViewModels;
using System.Linq;
using Avalonia.Styling;

namespace TicketSalesApp.UI.Avalonia.Views
{
    public partial class AuthWindow : Window
    {
        private AuthViewModel ViewModel => DataContext as AuthViewModel ?? throw new InvalidOperationException("DataContext is not AuthViewModel");

        public AuthWindow()
        {
            try
            {
                InitializeComponent();

                // Subscribe to authentication state changes when DataContext is set
                this.DataContextChanged += (s, e) =>
                {
                    if (DataContext is AuthViewModel vm)
                    {
                        vm.PropertyChanged += (sender, args) =>
                        {
                            if (args.PropertyName == nameof(AuthViewModel.IsAuthenticated) && vm.IsAuthenticated)
                            {
                                Close();
                            }
                        };
                    }
                };

                // Initial theme setup
                UpdateThemeStyles();

                // Subscribe to theme changes
                Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
                Application.Current.ActualThemeVariantChanged += (s, e) =>
                {
                    UpdateThemeStyles();
                };
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred during the initialization of AuthWindow.");
                ShowErrorBox(e.Message);
            }
        }

        private void UpdateThemeStyles()
        {
            try
            {
                var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
                var themeClass = isDark ? "dark" : "light";

                // Update TextBoxes
                foreach (var textBox in this.GetVisualDescendants().OfType<TextBox>())
                {
                    var existingClasses = textBox.Classes.ToList();
                    textBox.Classes.Clear();
                    foreach (var cls in existingClasses.Where(c => c != "light" && c != "dark"))
                    {
                        textBox.Classes.Add(cls);
                    }
                    textBox.Classes.Add(themeClass);
                }

                // Update TextBlocks
                foreach (var textBlock in this.GetVisualDescendants().OfType<TextBlock>())
                {
                    var existingClasses = textBlock.Classes.ToList();
                    textBlock.Classes.Clear();
                    foreach (var cls in existingClasses.Where(c => c != "light" && c != "dark"))
                    {
                        textBlock.Classes.Add(cls);
                    }
                    textBlock.Classes.Add(themeClass);
                }

                // Update Buttons
                foreach (var button in this.GetVisualDescendants().OfType<Button>())
                {
                    var existingClasses = button.Classes.ToList();
                    button.Classes.Clear();
                    foreach (var cls in existingClasses.Where(c => c != "light" && c != "dark"))
                    {
                        button.Classes.Add(cls);
                    }
                    button.Classes.Add(themeClass);
                }

                // Update loading overlay if it exists
                var loadingOverlay = this.GetVisualDescendants().OfType<Panel>()
                    .FirstOrDefault(p => p.Classes.Contains("loading-overlay"));
                if (loadingOverlay != null)
                {
                    var existingClasses = loadingOverlay.Classes.ToList();
                    loadingOverlay.Classes.Clear();
                    foreach (var cls in existingClasses.Where(c => c != "light" && c != "dark"))
                    {
                        loadingOverlay.Classes.Add(cls);
                    }
                    loadingOverlay.Classes.Add(themeClass);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating theme styles");
            }
        }

        private async void ShowErrorBox(string errorMessage)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", errorMessage,
                    ButtonEnum.Ok);

            await box.ShowAsync();
        }
    }
}
