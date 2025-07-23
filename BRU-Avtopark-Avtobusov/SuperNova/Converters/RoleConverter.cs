using System;
using System.Globalization;

namespace SuperNova.Converters
{
    public class RoleConverter : SafeConverterBase
    {
        protected override object? SafeConvert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int role)
            {
                return role == 1 ? "Admin" : "User";
            }
            return "Unknown";
        }

        protected override object? SafeConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string roleStr)
            {
                return roleStr == "Admin" ? 1 : 0;
            }
            return 0;
        }
    }
}