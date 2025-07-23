using System;
using System.Collections.Concurrent;
using System.Threading;
using Avalonia;
using Avalonia.Data;

namespace SuperNova.Converters;

/// <summary>
/// CRITICAL: Global protection against property change loops that cause stack overflow
/// </summary>
public static class PropertyChangeProtection
{
    private static readonly ThreadLocal<int> _propertyChangeDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, DateTime> _lastPropertyChangeTime = new();
    private static readonly ConcurrentDictionary<string, int> _propertyChangeCount = new();
    private static readonly TimeSpan _minPropertyChangeInterval = TimeSpan.FromMilliseconds(1);
    
    private const int MAX_PROPERTY_CHANGE_DEPTH = 3; // CRITICAL: Very low limit
    private const int MAX_PROPERTY_CHANGES_PER_SECOND = 50; // CRITICAL: Low limit
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    
    /// <summary>
    /// Initialize the property change protection system
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
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("PropertyChangeProtection initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize PropertyChangeProtection: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// CRITICAL: Check if property change should be allowed
    /// </summary>
    public static bool ShouldAllowPropertyChange(AvaloniaObject obj, AvaloniaProperty property, object? newValue)
    {
        if (!_isInitialized)
            return true;
            
        try
        {
            var operationKey = $"{obj.GetType().Name}_{property.Name}_{obj.GetHashCode()}";
            
            // Check property change depth - CRITICAL: Block deep recursion
            if (_propertyChangeDepth.Value >= MAX_PROPERTY_CHANGE_DEPTH)
            {
                System.Diagnostics.Debug.WriteLine($"Property change blocked due to depth limit: {operationKey}");
                return false;
            }
            
            // Check property change frequency - CRITICAL: Block rapid changes
            var now = DateTime.UtcNow;
            if (_lastPropertyChangeTime.TryGetValue(operationKey, out var lastTime))
            {
                if (now - lastTime < _minPropertyChangeInterval)
                {
                    System.Diagnostics.Debug.WriteLine($"Property change blocked due to frequency limit: {operationKey}");
                    return false;
                }
            }
            
            // Check property change count per second
            var countKey = $"{operationKey}_{now.Second}";
            var currentCount = _propertyChangeCount.AddOrUpdate(countKey, 1, (k, v) => v + 1);
            if (currentCount > MAX_PROPERTY_CHANGES_PER_SECOND)
            {
                System.Diagnostics.Debug.WriteLine($"Property change blocked due to count limit: {operationKey}");
                return false;
            }
            
            _lastPropertyChangeTime[operationKey] = now;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ShouldAllowPropertyChange: {ex.Message}");
            return true; // Allow on error to avoid breaking functionality
        }
    }
    
    /// <summary>
    /// CRITICAL: Safe property change execution with protection
    /// </summary>
    public static bool SafeSetProperty(AvaloniaObject obj, AvaloniaProperty property, object? value, BindingPriority priority = BindingPriority.LocalValue)
    {
        if (!ShouldAllowPropertyChange(obj, property, value))
            return false;
            
        _propertyChangeDepth.Value++;
        
        try
        {
            obj.SetValue(property, value, priority);
            return true;
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeSetProperty: {obj.GetType().Name}.{property.Name}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeSetProperty {obj.GetType().Name}.{property.Name}: {ex.Message}");
            return false;
        }
        finally
        {
            _propertyChangeDepth.Value--;
        }
    }
    
    /// <summary>
    /// CRITICAL: Safe property value conversion with protection
    /// </summary>
    public static object? SafeConvertPropertyValue(object? value, Type targetType, string operationKey)
    {
        if (_propertyChangeDepth.Value >= MAX_PROPERTY_CHANGE_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"Property value conversion blocked due to depth limit: {operationKey}");
            return GetDefaultValueForType(targetType);
        }
        
        _propertyChangeDepth.Value++;
        
        try
        {
            // Use our existing safe conversion
            return SuperNova.Converters.AvaloniaBindingInterceptor.SafeConvertValue(value, targetType, operationKey);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeConvertPropertyValue: {operationKey}");
            return GetDefaultValueForType(targetType);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeConvertPropertyValue {operationKey}: {ex.Message}");
            return GetDefaultValueForType(targetType);
        }
        finally
        {
            _propertyChangeDepth.Value--;
        }
    }
    
    /// <summary>
    /// Get default value for a type
    /// </summary>
    private static object? GetDefaultValueForType(Type type)
    {
        try
        {
            if (type == typeof(string))
                return "";
            if (type == typeof(int))
                return 0;
            if (type == typeof(double))
                return 0.0;
            if (type == typeof(bool))
                return false;
            if (type == typeof(DateTime))
                return DateTime.Now;
            if (type == typeof(DateTimeOffset))
                return DateTimeOffset.Now;
                
            if (type.IsValueType)
                return Activator.CreateInstance(type);
                
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Clear all cached data
    /// </summary>
    public static void ClearCache()
    {
        _lastPropertyChangeTime.Clear();
        _propertyChangeCount.Clear();
    }
    
    /// <summary>
    /// Get current property change depth for debugging
    /// </summary>
    public static int GetCurrentDepth()
    {
        return _propertyChangeDepth.Value;
    }
}

/// <summary>
/// CRITICAL: Extension methods for safe property operations
/// </summary>
public static class SafePropertyExtensions
{
    /// <summary>
    /// Safe property setting extension
    /// </summary>
    public static bool SafeSetValue(this AvaloniaObject obj, AvaloniaProperty property, object? value, BindingPriority priority = BindingPriority.LocalValue)
    {
        return PropertyChangeProtection.SafeSetProperty(obj, property, value, priority);
    }
    
    /// <summary>
    /// Safe property getting with protection
    /// </summary>
    public static T? SafeGetValue<T>(this AvaloniaObject obj, AvaloniaProperty<T> property)
    {
        try
        {
            if (PropertyChangeProtection.GetCurrentDepth() >= 3)
            {
                System.Diagnostics.Debug.WriteLine($"Property get blocked due to depth limit: {obj.GetType().Name}.{property.Name}");
                return default(T);
            }
            
            return (T)obj.GetValue(property);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeGetValue: {obj.GetType().Name}.{property.Name}");
            return default(T);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeGetValue {obj.GetType().Name}.{property.Name}: {ex.Message}");
            return default(T);
        }
    }
}
