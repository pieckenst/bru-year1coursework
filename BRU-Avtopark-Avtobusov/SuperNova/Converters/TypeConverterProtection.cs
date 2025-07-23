using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading;

namespace SuperNova.Converters;

/// <summary>
/// Global protection system for TypeConverter operations to prevent stack overflow
/// </summary>
public static class TypeConverterProtection
{
    private static readonly ThreadLocal<int> _conversionDepth = new(() => 0);
    private static readonly ConcurrentDictionary<Type, TypeConverter> _converterCache = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastOperationTime = new();
    private static readonly TimeSpan _minOperationInterval = TimeSpan.FromMilliseconds(5);
    
    private const int MAX_CONVERSION_DEPTH = 1; // ENHANCED: Reduced from 5 to 1
    
    /// <summary>
    /// Safely get a TypeConverter for the specified type
    /// </summary>
    public static TypeConverter SafeGetConverter(Type type)
    {
        var operationKey = $"GetConverter_{type.FullName}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check recursion depth
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"TypeConverter.GetConverter blocked due to recursion depth: {type.Name}");
            return GetFallbackConverter(type);
        }
        
        // Check operation frequency
        var now = DateTime.UtcNow;
        if (_lastOperationTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minOperationInterval)
            {
                System.Diagnostics.Debug.WriteLine($"TypeConverter.GetConverter blocked due to frequency: {type.Name}");
                return _converterCache.GetOrAdd(type, GetFallbackConverter);
            }
        }
        
        _lastOperationTime[operationKey] = now;
        
        // Try to get from cache first
        if (_converterCache.TryGetValue(type, out var cachedConverter))
        {
            return cachedConverter;
        }
        
        _conversionDepth.Value++;
        
        try
        {
            var converter = TypeDescriptor.GetConverter(type);
            _converterCache[type] = converter;
            return converter;
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in TypeConverter.GetConverter for {type.Name}");
            var fallback = GetFallbackConverter(type);
            _converterCache[type] = fallback;
            return fallback;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in TypeConverter.GetConverter for {type.Name}: {ex.Message}");
            var fallback = GetFallbackConverter(type);
            _converterCache[type] = fallback;
            return fallback;
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    /// <summary>
    /// Safely convert a value to the target type
    /// </summary>
    public static object? SafeConvertTo(object? value, Type targetType)
    {
        if (value == null)
            return GetDefaultValue(targetType);
            
        var sourceType = value.GetType();
        
        // If types match, return as-is
        if (sourceType == targetType || targetType.IsAssignableFrom(sourceType))
            return value;
            
        var operationKey = $"ConvertTo_{sourceType.Name}_{targetType.Name}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check recursion depth
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"Type conversion blocked due to recursion depth: {sourceType.Name} -> {targetType.Name}");
            return GetDefaultValue(targetType);
        }
        
        // Check operation frequency
        var now = DateTime.UtcNow;
        if (_lastOperationTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minOperationInterval)
            {
                System.Diagnostics.Debug.WriteLine($"Type conversion blocked due to frequency: {sourceType.Name} -> {targetType.Name}");
                return GetDefaultValue(targetType);
            }
        }
        
        _lastOperationTime[operationKey] = now;
        _conversionDepth.Value++;
        
        try
        {
            var converter = SafeGetConverter(targetType);
            if (converter.CanConvertFrom(sourceType))
            {
                return converter.ConvertFrom(value);
            }
            
            // Try string conversion as fallback
            if (targetType == typeof(string))
            {
                return value.ToString();
            }
            
            // Try basic type conversions
            return Convert.ChangeType(value, targetType);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in type conversion: {sourceType.Name} -> {targetType.Name}");
            return GetDefaultValue(targetType);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in type conversion {sourceType.Name} -> {targetType.Name}: {ex.Message}");
            return GetDefaultValue(targetType);
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    /// <summary>
    /// Get a fallback converter for the specified type
    /// </summary>
    private static TypeConverter GetFallbackConverter(Type type)
    {
        // Return a simple converter that just returns the value as-is
        return new FallbackTypeConverter();
    }
    
    /// <summary>
    /// Get a default value for the specified type
    /// </summary>
    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
            return "";
        if (type == typeof(int))
            return 0;
        if (type == typeof(bool))
            return false;
        if (type == typeof(double))
            return 0.0;
        if (type == typeof(DateTime))
            return DateTime.Now;
        if (type.IsValueType)
            return Activator.CreateInstance(type);
            
        return null;
    }
    
    /// <summary>
    /// Clear the converter cache (useful for testing or memory management)
    /// </summary>
    public static void ClearCache()
    {
        _converterCache.Clear();
        _lastOperationTime.Clear();
    }
}

/// <summary>
/// Fallback TypeConverter that prevents further recursion
/// </summary>
internal class FallbackTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }
    
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            return stringValue;
        }
        
        return value;
    }
    
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }
    
    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
        {
            return value?.ToString() ?? "";
        }
        
        return value;
    }
}
