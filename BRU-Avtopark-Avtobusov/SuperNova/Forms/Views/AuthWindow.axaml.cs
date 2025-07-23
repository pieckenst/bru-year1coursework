// UI/Avalonia/Views/AuthWindow.cs
using Avalonia.Controls;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System;
using Serilog;
using PleasantUI.Controls;
//using PleasantUI.Controls;

namespace SuperNova.Forms.Views
{
    public partial class AuthWindow : PleasantWindow 
    {
        public AuthWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred during the initialization of AuthWindow.");
                ShowErrorBox(e.Message);
            }
        }

        private async void ShowErrorBox(string errorMessage)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Error", errorMessage,
                    ButtonEnum.YesNo);

            var result = await box.ShowAsync();
        }
    }
}
