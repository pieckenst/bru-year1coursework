using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace TicketSalesApp.UI.Administration.Avalonia.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "approved" => new SolidColorBrush(Color.Parse("#4CAF50")), // Green
                    "pending" => new SolidColorBrush(Color.Parse("#FF9800")), // Orange
                    "rejected" => new SolidColorBrush(Color.Parse("#F44336")), // Red
                    "cancelled" => new SolidColorBrush(Color.Parse("#9E9E9E")), // Gray
                    _ => new SolidColorBrush(Color.Parse("#2196F3")) // Blue (default)
                };
            }
            return new SolidColorBrush(Color.Parse("#2196F3"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
