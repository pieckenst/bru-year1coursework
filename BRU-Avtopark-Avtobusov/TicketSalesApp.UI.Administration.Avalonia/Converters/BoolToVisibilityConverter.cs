using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace TicketSalesApp.UI.Administration.Avalonia.Converters
{
// Add this to your Converters folder
public class BoolToStringConverter : IMultiValueConverter //THIS IS FOR BOOL TO STRING FOR WINDOWS ACCOUNT LINKING
{
    public static readonly BoolToStringConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 3 && 
            values[0] is bool isVisible && 
            values[1] is string trueValue && 
            values[2] is string falseValue)
        {
            return isVisible ? trueValue : falseValue;
        }
        return values.Count > 2 ? values[2] : "Link Account";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// OTHER TWO ARE USED ELSEWHERE
    public class BoolToVisibilityConverter : IValueConverter 
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
