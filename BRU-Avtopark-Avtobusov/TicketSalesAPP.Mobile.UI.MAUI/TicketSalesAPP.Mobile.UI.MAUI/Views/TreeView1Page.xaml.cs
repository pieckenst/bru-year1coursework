using System.Globalization;

namespace TicketSalesAPP.Mobile.UI.MAUI.Views
{
    public partial class TreeView1Page : ContentPage
    {
        public TreeView1Page()
        {
            InitializeComponent();
        }
    }

    public class BoolToImageSourceConverter : IValueConverter
    {
        public ImageSource? FalseSource { get; set; }
        public ImageSource? TrueSource { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return null;

            return (bool)value ? TrueSource : FalseSource;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}