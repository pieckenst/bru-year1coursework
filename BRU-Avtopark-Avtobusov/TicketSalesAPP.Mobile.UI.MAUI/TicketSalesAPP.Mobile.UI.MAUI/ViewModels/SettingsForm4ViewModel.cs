using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Maui.Core;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class SettingsForm4ViewModel : ObservableObject
    {

        [ObservableProperty]
        private bool darkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

        [ObservableProperty]
        private string language = "English";

        [ObservableProperty]
        private IEnumerable<string> languages = new List<string> {
            "English", "German", "French", "Spanish", "Italian"
        };

        [ObservableProperty]
        public List<ColorModel> themes;

        [ObservableProperty]
        public string? previewColorName;

        [ObservableProperty]
        public int selectedColorIndex;

        public SettingsForm4ViewModel()
        {
            SelectedColorIndex = ON.Platform(1, 0);
            Themes = new List<ColorModel>() {
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.Purple), ThemeSeedColor.Purple.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.Violet), ThemeSeedColor.Violet.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.Red), ThemeSeedColor.Red.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.Brown), ThemeSeedColor.Brown.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.TealGreen), ThemeSeedColor.TealGreen.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.Green), ThemeSeedColor.Green.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.DarkGreen), ThemeSeedColor.DarkGreen.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.DarkCyan), ThemeSeedColor.DarkCyan.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.DeepSeaBlue), ThemeSeedColor.DeepSeaBlue.ToString()),
                new ColorModel(ThemeManager.GetSeedColor(ThemeSeedColor.Blue), ThemeSeedColor.Blue.ToString()),
            };
            PreviewColorName = Themes[SelectedColorIndex].DisplayName;
        }

        [RelayCommand]
        public void ChangeColor()
        {
            var colorModel = Themes[SelectedColorIndex];
            if (colorModel == null)
                return;

            PreviewColorName = colorModel.DisplayName;
            if (colorModel.IsSystemColor)
            {
                ThemeManager.UseAndroidSystemColor = true;
                return;
            }

            ThemeManager.UseAndroidSystemColor = false;
            ThemeManager.Theme = new Theme(colorModel.Color);
        }

        partial void OnDarkModeChanged(bool value)
        {
            if (Application.Current == null)
                return;

            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }
    }

    public class ColorModel
    {
        public Color Color { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsSystemColor { get; set; }

        public ColorModel(Color color, string displayName, bool isSystemColor = false)
        {
            Color = color;
            DisplayName = displayName;
            IsSystemColor = isSystemColor;
            Name = isSystemColor ? "System" : string.Empty;
        }
    }
}