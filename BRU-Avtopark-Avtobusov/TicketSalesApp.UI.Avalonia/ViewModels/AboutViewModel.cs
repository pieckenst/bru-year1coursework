using ReactiveUI;
using System;
using System.Reflection;

namespace TicketSalesApp.UI.Avalonia.ViewModels
{
    public class AboutViewModel : ReactiveObject
    {
        private string _version;
        public string Version
        {
            get => _version;
            set => this.RaiseAndSetIfChanged(ref _version, value);
        }

        private string _runtimeVersion;
        public string RuntimeVersion
        {
            get => _runtimeVersion;
            set => this.RaiseAndSetIfChanged(ref _runtimeVersion, value);
        }

        public AboutViewModel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            RuntimeVersion = Environment.Version.ToString();
        }
    }
} 