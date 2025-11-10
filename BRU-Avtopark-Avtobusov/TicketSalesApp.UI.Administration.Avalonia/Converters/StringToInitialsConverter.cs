using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TicketSalesApp.UI.Administration.Avalonia.Converters
{
    public class StringToInitialsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                return text[0].ToString().ToUpper();
            }
            return "?";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
