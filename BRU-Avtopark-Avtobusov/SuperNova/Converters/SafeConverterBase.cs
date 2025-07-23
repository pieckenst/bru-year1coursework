using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using Avalonia.Data.Converters;

namespace SuperNova.Converters;

/// <summary>
/// Base class for safe value converters that prevent recursive conversion loops and stack overflow
/// </summary>
public abstract class SafeConverterBase : IValueConverter
{
    private static readonly ConcurrentDictionary<string, DateTime> _lastConversionTime = new();
    private static readonly ThreadLocal<int> _conversionDepth = new(() => 0);
    private static readonly TimeSpan _minConversionInterval = TimeSpan.FromMilliseconds(1);
    
    private const int MAX_CONVERSION_DEPTH = 1; // ENHANCED: Reduced from 3 to 1
    
    /// <summary>
    /// Unique identifier for this converter instance
    /// </summary>
    protected virtual string ConverterId => GetType().Name;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var operationKey = $"{ConverterId}_Convert_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check conversion depth to prevent recursion
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"Conversion blocked due to depth limit: {ConverterId}");
            return GetSafeDefaultValue(targetType, value);
        }
        
        // Check frequency to prevent rapid conversion loops
        var now = DateTime.UtcNow;
        if (_lastConversionTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minConversionInterval)
            {
                System.Diagnostics.Debug.WriteLine($"Conversion blocked due to frequency limit: {ConverterId}");
                return GetSafeDefaultValue(targetType, value);
            }
        }
        
        _lastConversionTime[operationKey] = now;
        _conversionDepth.Value++;
        
        try
        {
            return SafeConvert(value, targetType, parameter, culture);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in converter: {ConverterId}");
            return GetSafeDefaultValue(targetType, value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in converter {ConverterId}: {ex.Message}");
            return GetSafeDefaultValue(targetType, value);
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var operationKey = $"{ConverterId}_ConvertBack_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check conversion depth to prevent recursion
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"ConvertBack blocked due to depth limit: {ConverterId}");
            return GetSafeDefaultValue(targetType, value);
        }
        
        // Check frequency to prevent rapid conversion loops
        var now = DateTime.UtcNow;
        if (_lastConversionTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minConversionInterval)
            {
                System.Diagnostics.Debug.WriteLine($"ConvertBack blocked due to frequency limit: {ConverterId}");
                return GetSafeDefaultValue(targetType, value);
            }
        }
        
        _lastConversionTime[operationKey] = now;
        _conversionDepth.Value++;
        
        try
        {
            return SafeConvertBack(value, targetType, parameter, culture);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in ConvertBack: {ConverterId}");
            return GetSafeDefaultValue(targetType, value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ConvertBack {ConverterId}: {ex.Message}");
            return GetSafeDefaultValue(targetType, value);
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    /// <summary>
    /// Implement the actual conversion logic here
    /// </summary>
    protected abstract object? SafeConvert(object? value, Type targetType, object? parameter, CultureInfo culture);
    
    /// <summary>
    /// Implement the actual convert back logic here
    /// </summary>
    protected virtual object? SafeConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException($"ConvertBack not implemented for {GetType().Name}");
    }
    
    /// <summary>
    /// Get a safe default value when conversion fails
    /// </summary>
    protected virtual object? GetSafeDefaultValue(Type targetType, object? originalValue)
    {
        if (targetType == typeof(string))
            return originalValue?.ToString() ?? "";
        if (targetType == typeof(int))
            return 0;
        if (targetType == typeof(bool))
            return false;
        if (targetType == typeof(double))
            return 0.0;
        if (targetType == typeof(DateTime))
            return DateTime.Now;
        
        // Return original value if we can't provide a better default
        return originalValue;
    }
}
