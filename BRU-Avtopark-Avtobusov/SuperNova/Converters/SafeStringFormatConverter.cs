using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;

namespace SuperNova.Converters;

/// <summary>
/// Safe string format converter that prevents recursive formatting loops and stack overflow
/// </summary>
public class SafeStringFormatConverter : SafeConverterBase
{
    private static readonly ConcurrentDictionary<string, DateTime> _lastFormatTime = new();
    private static readonly ThreadLocal<int> _formatDepth = new(() => 0);
    private static readonly TimeSpan _minFormatInterval = TimeSpan.FromMilliseconds(5);
    
    private const int MAX_FORMAT_DEPTH = 2;
    
    public string Format { get; set; } = "";
    
    protected override string ConverterId => $"SafeStringFormat_{Format}";
    
    protected override object? SafeConvert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return "";
            
        var operationKey = $"Format_{Format}_{value.GetType().Name}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check format depth to prevent recursion
        if (_formatDepth.Value >= MAX_FORMAT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"String formatting blocked due to depth limit: {Format}");
            return GetSafeFormattedValue(value);
        }
        
        // Check format frequency
        var now = DateTime.UtcNow;
        if (_lastFormatTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minFormatInterval)
            {
                System.Diagnostics.Debug.WriteLine($"String formatting blocked due to frequency limit: {Format}");
                return GetSafeFormattedValue(value);
            }
        }
        
        _lastFormatTime[operationKey] = now;
        _formatDepth.Value++;
        
        try
        {
            // Handle different value types safely
            if (value is DateTime dateTime)
            {
                return FormatDateTime(dateTime, Format, culture);
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {
                return FormatDateTime(dateTimeOffset.DateTime, Format, culture);
            }
            else if (value is decimal decimalValue)
            {
                return FormatDecimal(decimalValue, Format, culture);
            }
            else if (value is double doubleValue)
            {
                return FormatDouble(doubleValue, Format, culture);
            }
            else if (value is int intValue)
            {
                return FormatInt(intValue, Format, culture);
            }
            else
            {
                // Fallback to simple string formatting
                if (!string.IsNullOrEmpty(Format))
                {
                    return string.Format(culture, Format, value);
                }
                return value.ToString() ?? "";
            }
        }
        catch (FormatException)
        {
            System.Diagnostics.Debug.WriteLine($"Format exception in SafeStringFormatConverter: {Format}");
            return GetSafeFormattedValue(value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeStringFormatConverter: {ex.Message}");
            return GetSafeFormattedValue(value);
        }
        finally
        {
            _formatDepth.Value--;
        }
    }
    
    private static string FormatDateTime(DateTime dateTime, string format, CultureInfo culture)
    {
        try
        {
            // Use safe DateTime formatting patterns
            if (format.Contains("dd.MM.yyyy HH:mm"))
                return dateTime.ToString("dd.MM.yyyy HH:mm", culture);
            else if (format.Contains("dd.MM.yyyy"))
                return dateTime.ToString("dd.MM.yyyy", culture);
            else if (format.Contains("yyyy-MM-dd"))
                return dateTime.ToString("yyyy-MM-dd", culture);
            else
                return dateTime.ToString("yyyy-MM-dd HH:mm", culture);
        }
        catch
        {
            return dateTime.ToString("yyyy-MM-dd", culture);
        }
    }
    
    private static string FormatDecimal(decimal value, string format, CultureInfo culture)
    {
        try
        {
            if (format.Contains("C"))
                return value.ToString("C", culture);
            else if (format.Contains("N"))
                return value.ToString("N2", culture);
            else
                return value.ToString("F2", culture);
        }
        catch
        {
            return value.ToString("F2", culture);
        }
    }
    
    private static string FormatDouble(double value, string format, CultureInfo culture)
    {
        try
        {
            if (format.Contains("C"))
                return value.ToString("C", culture);
            else if (format.Contains("N"))
                return value.ToString("N2", culture);
            else
                return value.ToString("F2", culture);
        }
        catch
        {
            return value.ToString("F2", culture);
        }
    }
    
    private static string FormatInt(int value, string format, CultureInfo culture)
    {
        try
        {
            if (format.Contains("N"))
                return value.ToString("N0", culture);
            else
                return value.ToString(culture);
        }
        catch
        {
            return value.ToString();
        }
    }
    
    private static string GetSafeFormattedValue(object? value)
    {
        if (value == null)
            return "";
            
        try
        {
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd");
            else if (value is DateTimeOffset dto)
                return dto.ToString("yyyy-MM-dd");
            else if (value is decimal dec)
                return dec.ToString("F2");
            else if (value is double dbl)
                return dbl.ToString("F2");
            else
                return value.ToString() ?? "";
        }
        catch
        {
            return value.GetType().Name;
        }
    }
}

/// <summary>
/// Safe DateTime format converter specifically for DataGrid DateTime columns
/// </summary>
public class SafeDateTimeFormatConverter : SafeConverterBase
{
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    
    protected override object? SafeConvert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return "";
            
        try
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString(DateFormat, culture);
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.DateTime.ToString(DateFormat, culture);
            }
            else
            {
                return value.ToString() ?? "";
            }
        }
        catch
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return Avalonia.AvaloniaProperty.UnsetValue;

        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
        {
            if (value is string str && str.Length == DateFormat.Length && DateTime.TryParseExact(str, DateFormat, culture, DateTimeStyles.None, out var result))
                return result;
            return Avalonia.AvaloniaProperty.UnsetValue;
        }
        if (targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset?))
        {
            if (value is string str && str.Length == DateFormat.Length && DateTimeOffset.TryParseExact(str, DateFormat, culture, DateTimeStyles.None, out var result))
                return result;
            return Avalonia.AvaloniaProperty.UnsetValue;
        }
        return Avalonia.AvaloniaProperty.UnsetValue;
    }
}

/// <summary>
/// Safe currency format converter specifically for DataGrid currency columns
/// </summary>
public class SafeCurrencyFormatConverter : SafeConverterBase
{
    protected override object? SafeConvert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return "0.00";
            
        try
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("F2", culture);
            }
            else if (value is double doubleValue)
            {
                return doubleValue.ToString("F2", culture);
            }
            else if (value is int intValue)
            {
                return intValue.ToString("F2", culture);
            }
            else
            {
                return value.ToString() ?? "0.00";
            }
        }
        catch
        {
            return "0.00";
        }
    }
}
