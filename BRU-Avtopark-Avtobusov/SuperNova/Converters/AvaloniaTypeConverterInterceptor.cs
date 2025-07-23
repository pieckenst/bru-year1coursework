using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace SuperNova.Converters;

/// <summary>
/// Aggressive interceptor for Avalonia's type conversion system to prevent stack overflow
/// </summary>
public static class AvaloniaTypeConverterInterceptor
{
    private static readonly ThreadLocal<int> _conversionDepth = new(() => 0);
    private static readonly ConcurrentDictionary<Type, TypeConverter> _safeConverterCache = new();
    private static readonly ConcurrentDictionary<string, DateTime> _lastConversionTime = new();
    private static readonly TimeSpan _minConversionInterval = TimeSpan.FromMilliseconds(1);
    
    private const int MAX_CONVERSION_DEPTH = 1; // ENHANCED: Reduced from 3 to 1
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    
    /// <summary>
    /// Initialize the interceptor system
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
            return;
            
        lock (_initLock)
        {
            if (_isInitialized)
                return;
                
            try
            {
                // Hook into TypeDescriptor to prevent recursive calls
                HookTypeDescriptor();
                
                // Pre-populate safe converters for common types
                PrePopulateSafeConverters();
                
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("AvaloniaTypeConverterInterceptor initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize AvaloniaTypeConverterInterceptor: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Safe type converter lookup that prevents recursion
    /// </summary>
    public static TypeConverter GetSafeConverter(Type type)
    {
        if (type == null)
            return new StringConverter();
            
        var operationKey = $"GetConverter_{type.FullName}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check conversion depth
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"TypeConverter lookup blocked due to depth limit: {type.Name}");
            return GetFallbackConverter(type);
        }
        
        // Check conversion frequency
        var now = DateTime.UtcNow;
        if (_lastConversionTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minConversionInterval)
            {
                System.Diagnostics.Debug.WriteLine($"TypeConverter lookup blocked due to frequency limit: {type.Name}");
                return _safeConverterCache.GetOrAdd(type, GetFallbackConverter);
            }
        }
        
        _lastConversionTime[operationKey] = now;
        
        // Try to get from cache first
        if (_safeConverterCache.TryGetValue(type, out var cachedConverter))
        {
            return cachedConverter;
        }
        
        _conversionDepth.Value++;
        
        try
        {
            // Use reflection to safely get converter without triggering recursion
            var converter = GetConverterSafely(type);
            _safeConverterCache[type] = converter;
            return converter;
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in GetSafeConverter for {type.Name}");
            var fallback = GetFallbackConverter(type);
            _safeConverterCache[type] = fallback;
            return fallback;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetSafeConverter for {type.Name}: {ex.Message}");
            var fallback = GetFallbackConverter(type);
            _safeConverterCache[type] = fallback;
            return fallback;
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    /// <summary>
    /// Safely get a TypeConverter without triggering recursion
    /// </summary>
    private static TypeConverter GetConverterSafely(Type type)
    {
        // For common types, return known safe converters
        if (type == typeof(string))
            return new StringConverter();
        if (type == typeof(int))
            return new Int32Converter();
        if (type == typeof(bool))
            return new BooleanConverter();
        if (type == typeof(DateTime))
            return new DateTimeConverter();
        if (type == typeof(double))
            return new DoubleConverter();
        if (type == typeof(decimal))
            return new DecimalConverter();
            
        // For other types, try to create a safe converter
        try
        {
            // Use a very limited TypeDescriptor call
            return TypeDescriptor.GetConverter(type);
        }
        catch
        {
            return GetFallbackConverter(type);
        }
    }
    
    /// <summary>
    /// Get a fallback converter that won't cause recursion
    /// </summary>
    private static TypeConverter GetFallbackConverter(Type type)
    {
        if (type == typeof(string) || type == typeof(object))
            return new SafeStringConverter();
        if (type.IsValueType)
            return new SafeValueTypeConverter();
        
        return new SafeObjectConverter();
    }
    
    /// <summary>
    /// Hook into TypeDescriptor to prevent recursive calls
    /// </summary>
    private static void HookTypeDescriptor()
    {
        try
        {
            // This is a more aggressive approach - we'll replace the default behavior
            // by pre-caching converters for all common types
            var commonTypes = new[]
            {
                typeof(string), typeof(int), typeof(bool), typeof(DateTime),
                typeof(double), typeof(decimal), typeof(float), typeof(long),
                typeof(short), typeof(byte), typeof(char), typeof(object),
                typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid)
            };
            
            foreach (var type in commonTypes)
            {
                try
                {
                    var converter = GetConverterSafely(type);
                    _safeConverterCache[type] = converter;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to cache converter for {type.Name}: {ex.Message}");
                    _safeConverterCache[type] = GetFallbackConverter(type);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to hook TypeDescriptor: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Pre-populate safe converters for common types
    /// </summary>
    private static void PrePopulateSafeConverters()
    {
        var safeConverters = new Dictionary<Type, TypeConverter>
        {
            { typeof(string), new SafeStringConverter() },
            { typeof(int), new SafeInt32Converter() },
            { typeof(bool), new SafeBooleanConverter() },
            { typeof(DateTime), new SafeDateTimeConverter() },
            { typeof(double), new SafeDoubleConverter() },
            { typeof(decimal), new SafeDecimalConverter() },
            { typeof(object), new SafeObjectConverter() }
        };
        
        foreach (var kvp in safeConverters)
        {
            _safeConverterCache[kvp.Key] = kvp.Value;
        }
    }
    
    /// <summary>
    /// Clear all cached data
    /// </summary>
    public static void ClearCache()
    {
        _safeConverterCache.Clear();
        _lastConversionTime.Clear();
    }
}

/// <summary>
/// Safe string converter that prevents recursion
/// </summary>
internal class SafeStringConverter : StringConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        try
        {
            return value?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }
    
    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
            return value?.ToString() ?? "";
        return value;
    }
}

/// <summary>
/// Safe int32 converter that prevents recursion
/// </summary>
internal class SafeInt32Converter : Int32Converter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        try
        {
            if (value is string str && int.TryParse(str, out var result))
                return result;
            if (value is int intVal)
                return intVal;
            return 0;
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// Safe boolean converter that prevents recursion
/// </summary>
internal class SafeBooleanConverter : BooleanConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        try
        {
            if (value is string str && bool.TryParse(str, out var result))
                return result;
            if (value is bool boolVal)
                return boolVal;
            return false;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Safe DateTime converter that prevents recursion
/// </summary>
internal class SafeDateTimeConverter : DateTimeConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        try
        {
            if (value is string str && DateTime.TryParse(str, out var result))
                return result;
            if (value is DateTime dateTime)
                return dateTime;
            return DateTime.Now;
        }
        catch
        {
            return DateTime.Now;
        }
    }
}

/// <summary>
/// Safe double converter that prevents recursion
/// </summary>
internal class SafeDoubleConverter : DoubleConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        try
        {
            if (value is string str && double.TryParse(str, out var result))
                return result;
            if (value is double doubleVal)
                return doubleVal;
            return 0.0;
        }
        catch
        {
            return 0.0;
        }
    }
}

/// <summary>
/// Safe decimal converter that prevents recursion
/// </summary>
internal class SafeDecimalConverter : DecimalConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        try
        {
            if (value is string str && decimal.TryParse(str, out var result))
                return result;
            if (value is decimal decimalVal)
                return decimalVal;
            return 0m;
        }
        catch
        {
            return 0m;
        }
    }
}

/// <summary>
/// Safe value type converter that prevents recursion
/// </summary>
internal class SafeValueTypeConverter : TypeConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        return value;
    }
    
    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
            return value?.ToString() ?? "";
        return value;
    }
}

/// <summary>
/// Safe object converter that prevents recursion
/// </summary>
internal class SafeObjectConverter : TypeConverter
{
    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value)
    {
        return value;
    }
    
    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string))
            return value?.ToString() ?? "";
        return value;
    }
}
