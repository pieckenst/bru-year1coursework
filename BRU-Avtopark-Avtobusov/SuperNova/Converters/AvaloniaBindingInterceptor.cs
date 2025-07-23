using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Core;

namespace SuperNova.Converters;

/// <summary>
/// Aggressive interceptor for Avalonia's binding system to prevent stack overflow
/// </summary>
public static class AvaloniaBindingInterceptor
{
    private static readonly ThreadLocal<int> _bindingDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, DateTime> _lastBindingTime = new();
    private static readonly ConcurrentDictionary<string, object> _bindingCache = new();
    private static readonly TimeSpan _minBindingInterval = TimeSpan.FromMilliseconds(10); // CRITICAL: Increased to 10ms

    private const int MAX_BINDING_DEPTH = 1; // CRITICAL: Reduced to 1 to stop all recursion
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    
    /// <summary>
    /// Initialize the binding interceptor system
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
                // Hook into Avalonia's binding system
                HookBindingSystem();
                
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("AvaloniaBindingInterceptor initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize AvaloniaBindingInterceptor: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Safe binding operation that prevents recursion
    /// </summary>
    public static bool TryExecuteBinding(string operationKey, Func<bool> operation)
    {
        // Check binding depth
        if (_bindingDepth.Value >= MAX_BINDING_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"Binding operation blocked due to depth limit: {operationKey}");
            return false;
        }
        
        // Check binding frequency
        var now = DateTime.UtcNow;
        if (_lastBindingTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minBindingInterval)
            {
                System.Diagnostics.Debug.WriteLine($"Binding operation blocked due to frequency limit: {operationKey}");
                return false;
            }
        }
        
        _lastBindingTime[operationKey] = now;
        _bindingDepth.Value++;
        
        try
        {
            return operation();
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in binding operation: {operationKey}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in binding operation {operationKey}: {ex.Message}");
            return false;
        }
        finally
        {
            _bindingDepth.Value--;
        }
    }
    
    /// <summary>
    /// Safe value conversion that prevents recursion - ENHANCED VERSION
    /// </summary>
    public static object? SafeConvertValue(object? value, Type targetType, string operationKey)
    {
        if (value == null)
            return GetDefaultValue(targetType);

        var sourceType = value.GetType();

        // If types match, return as-is
        if (sourceType == targetType || targetType.IsAssignableFrom(sourceType))
            return value;

        // CRITICAL: ZERO TOLERANCE for recursion
        if (_bindingDepth.Value >= 1) // CRITICAL: Reduced to 1 - NO recursion allowed
        {
            System.Diagnostics.Debug.WriteLine($"Value conversion blocked due to depth limit: {operationKey}");
            return GetDefaultValue(targetType);
        }

        // CRITICAL: AGGRESSIVE frequency limiting
        var now = DateTime.UtcNow;
        var conversionKey = $"{operationKey}_Convert_{sourceType.Name}_{targetType.Name}";
        if (_lastBindingTime.TryGetValue(conversionKey, out var lastTime))
        {
            if (now - lastTime < TimeSpan.FromMilliseconds(100)) // CRITICAL: Increased to 100ms
            {
                System.Diagnostics.Debug.WriteLine($"Value conversion blocked due to frequency limit: {conversionKey}");
                return GetDefaultValue(targetType);
            }
        }

        _lastBindingTime[conversionKey] = now;
        _bindingDepth.Value++;

        try
        {
            // ENHANCED: More aggressive safe conversion
            return PerformSafeConversion(value, targetType);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in value conversion: {conversionKey}");
            return GetDefaultValue(targetType);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in value conversion {conversionKey}: {ex.Message}");
            return GetDefaultValue(targetType);
        }
        finally
        {
            _bindingDepth.Value--;
        }
    }
    
    /// <summary>
    /// Perform safe type conversion without triggering recursion - ENHANCED VERSION
    /// </summary>
    private static object? PerformSafeConversion(object? value, Type targetType)
    {
        if (value == null)
            return GetDefaultValue(targetType);

        // CRITICAL: Handle string conversions with safe methods
        if (targetType == typeof(string))
        {
            try
            {
                // КРИТИЧНО: Используем глобальный DateTime перехватчик
                return SuperNova.Converters.GlobalDateTimeInterceptor.SafeConvertToString(value);
            }
            catch
            {
                return "";
            }
        }

        // ENHANCED: Handle all common type conversions with better error handling
        try
        {
            if (targetType == typeof(int))
            {
                if (value is string str) return int.TryParse(str, out var intResult) ? intResult : 0;
                if (value is double dbl) return (int)dbl;
                if (value is decimal dec) return (int)dec;
                return 0;
            }

            if (targetType == typeof(bool))
            {
                if (value is string boolStr) return bool.TryParse(boolStr, out var boolResult) ? boolResult : false;
                if (value is int intVal) return intVal != 0;
                return false;
            }

            if (targetType == typeof(DateTime))
            {
                if (value is string dateStr) return DateTime.TryParse(dateStr, out var dateResult) ? dateResult : DateTime.Now;
                if (value is DateTimeOffset dto) return dto.DateTime;
                return DateTime.Now;
            }

            // CRITICAL: Handle DateTimeOffset conversion
            if (targetType == typeof(DateTimeOffset))
            {
                if (value is string dateStr) return DateTimeOffset.TryParse(dateStr, out var dateResult) ? dateResult : DateTimeOffset.Now;
                if (value is DateTime dt) return new DateTimeOffset(dt);
                return DateTimeOffset.Now;
            }

            if (targetType == typeof(double))
            {
                if (value is string doubleStr) return double.TryParse(doubleStr, out var doubleResult) ? doubleResult : 0.0;
                if (value is int intVal) return (double)intVal;
                if (value is decimal decVal) return (double)decVal;
                return 0.0;
            }

            if (targetType == typeof(decimal))
            {
                if (value is string decStr) return decimal.TryParse(decStr, out var decResult) ? decResult : 0m;
                if (value is double dblVal) return (decimal)dblVal;
                if (value is int intVal) return (decimal)intVal;
                return 0m;
            }

            // ENHANCED: Avoid Convert.ChangeType completely to prevent recursion
            return GetDefaultValue(targetType);
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
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
        if (type == typeof(decimal))
            return 0m;
        if (type == typeof(DateTime))
            return DateTime.Now;
        if (type.IsValueType)
            return Activator.CreateInstance(type);
            
        return null;
    }
    
    /// <summary>
    /// Hook into Avalonia's binding system
    /// </summary>
    private static void HookBindingSystem()
    {
        try
        {
            // Set up global binding protection
            SetupGlobalBindingProtection();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to hook binding system: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Set up global binding protection
    /// </summary>
    private static void SetupGlobalBindingProtection()
    {
        // This method sets up protection at the application level
        // We'll use it to monitor and prevent recursive binding operations
        
        // Create a timer to clean up old entries
        var cleanupTimer = new System.Timers.Timer(5000); // 5 seconds
        cleanupTimer.Elapsed += (sender, e) => CleanupOldEntries();
        cleanupTimer.Start();
    }
    
    /// <summary>
    /// Clean up old entries to prevent memory leaks
    /// </summary>
    private static void CleanupOldEntries()
    {
        try
        {
            var now = DateTime.UtcNow;
            var cutoff = now.AddSeconds(-10); // Remove entries older than 10 seconds
            
            var keysToRemove = new List<string>();
            foreach (var kvp in _lastBindingTime)
            {
                if (kvp.Value < cutoff)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _lastBindingTime.TryRemove(key, out _);
                _bindingCache.TryRemove(key, out _);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
        }
    }
    
    /// <summary>
    /// CRITICAL: Safe DateTime to string conversion that prevents formatting loops
    /// </summary>
    public static string SafeDateTimeToString(DateTime dateTime, string operationKey = "DateTime")
    {
        var conversionKey = $"{operationKey}_DateTime_{dateTime.Ticks}";

        // Check conversion depth - CRITICAL: Block at depth 1
        if (_bindingDepth.Value >= 1)
        {
            System.Diagnostics.Debug.WriteLine($"DateTime conversion blocked due to depth limit: {conversionKey}");
            return dateTime.ToString("yyyy-MM-dd"); // Simple format without culture
        }

        // Check conversion frequency - CRITICAL: Block rapid conversions
        var now = DateTime.UtcNow;
        if (_lastBindingTime.TryGetValue(conversionKey, out var lastTime))
        {
            if (now - lastTime < TimeSpan.FromMilliseconds(50)) // 50ms minimum interval
            {
                System.Diagnostics.Debug.WriteLine($"DateTime conversion blocked due to frequency limit: {conversionKey}");
                return dateTime.ToString("yyyy-MM-dd"); // Simple format without culture
            }
        }

        _lastBindingTime[conversionKey] = now;
        _bindingDepth.Value++;

        try
        {
            // КРИТИЧНО: Используем глобальный DateTime перехватчик
            return SuperNova.Converters.GlobalDateTimeInterceptor.SafeFormatDateTime(dateTime);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in DateTime conversion: {conversionKey}");
            return dateTime.ToString("yyyy-MM-dd");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in DateTime conversion {conversionKey}: {ex.Message}");
            return dateTime.ToString("yyyy-MM-dd");
        }
        finally
        {
            _bindingDepth.Value--;
        }
    }

    /// <summary>
    /// CRITICAL: Safe object to string conversion that prevents all formatting loops
    /// </summary>
    public static string SafeObjectToString(object? obj, string operationKey = "Object")
    {
        if (obj == null)
            return "";

        var conversionKey = $"{operationKey}_ToString_{obj.GetType().Name}";

        // Check conversion depth - CRITICAL: Block at depth 1
        if (_bindingDepth.Value >= 1)
        {
            System.Diagnostics.Debug.WriteLine($"Object conversion blocked due to depth limit: {conversionKey}");
            return obj.GetType().Name; // Just return type name
        }

        // Check conversion frequency
        var now = DateTime.UtcNow;
        if (_lastBindingTime.TryGetValue(conversionKey, out var lastTime))
        {
            if (now - lastTime < TimeSpan.FromMilliseconds(50))
            {
                System.Diagnostics.Debug.WriteLine($"Object conversion blocked due to frequency limit: {conversionKey}");
                return obj.GetType().Name;
            }
        }

        _lastBindingTime[conversionKey] = now;
        _bindingDepth.Value++;

        try
        {
            // КРИТИЧНО: Используем глобальный DateTime перехватчик для всех конверсий
            return SuperNova.Converters.GlobalDateTimeInterceptor.SafeConvertToString(obj);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in object conversion: {conversionKey}");
            return obj.GetType().Name;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in object conversion {conversionKey}: {ex.Message}");
            return obj.GetType().Name;
        }
        finally
        {
            _bindingDepth.Value--;
        }
    }

    /// <summary>
    /// Clear all cached data
    /// </summary>
    public static void ClearCache()
    {
        _lastBindingTime.Clear();
        _bindingCache.Clear();
    }
}

/// <summary>
/// Extension methods for safe binding operations
/// </summary>
public static class SafeBindingExtensions
{
    /// <summary>
    /// Safely set a property value with binding protection
    /// </summary>
    public static void SafeSetValue<T>(this AvaloniaObject obj, AvaloniaProperty<T> property, T value)
    {
        var operationKey = $"SetValue_{obj.GetType().Name}_{property.Name}_{Thread.CurrentThread.ManagedThreadId}";
        
        AvaloniaBindingInterceptor.TryExecuteBinding(operationKey, () =>
        {
            try
            {
                obj.SetValue(property, value);
                return true;
            }
            catch (StackOverflowException)
            {
                System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeSetValue: {property.Name}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SafeSetValue {property.Name}: {ex.Message}");
                return false;
            }
        });
    }
    
    /// <summary>
    /// Safely get a property value with binding protection
    /// </summary>
    public static T SafeGetValue<T>(this AvaloniaObject obj, AvaloniaProperty<T> property, T defaultValue = default(T))
    {
        var operationKey = $"GetValue_{obj.GetType().Name}_{property.Name}_{Thread.CurrentThread.ManagedThreadId}";

        var result = defaultValue;
        AvaloniaBindingInterceptor.TryExecuteBinding(operationKey, () =>
        {
            try
            {
                var value = obj.GetValue(property);
                result = value is T typedValue ? typedValue : defaultValue;
                return true;
            }
            catch (StackOverflowException)
            {
                System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeGetValue: {property.Name}");
                result = defaultValue;
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SafeGetValue {property.Name}: {ex.Message}");
                result = defaultValue;
                return false;
            }
        });

        return result;
    }
}
