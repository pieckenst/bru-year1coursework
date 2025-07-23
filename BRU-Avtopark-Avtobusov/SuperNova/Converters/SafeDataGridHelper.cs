using System;
using System.Collections.Concurrent;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace SuperNova.Converters;

/// <summary>
/// Helper class to create safe DataGrid bindings that prevent stack overflow
/// </summary>
public static class SafeDataGridHelper
{
    private static readonly ThreadLocal<int> _bindingDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, DateTime> _lastBindingTime = new();
    private static readonly TimeSpan _minBindingInterval = TimeSpan.FromMilliseconds(10);
    
    private const int MAX_BINDING_DEPTH = 3;
    
    /// <summary>
    /// Create a safe binding for DataGrid columns
    /// </summary>
    public static Binding CreateSafeBinding(string path, IValueConverter? converter = null)
    {
        var operationKey = $"CreateBinding_{path}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check binding depth
        if (_bindingDepth.Value >= MAX_BINDING_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"DataGrid binding blocked due to depth limit: {path}");
            return CreateFallbackBinding(path);
        }
        
        // Check binding frequency
        var now = DateTime.UtcNow;
        if (_lastBindingTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minBindingInterval)
            {
                System.Diagnostics.Debug.WriteLine($"DataGrid binding blocked due to frequency limit: {path}");
                return CreateFallbackBinding(path);
            }
        }
        
        _lastBindingTime[operationKey] = now;
        _bindingDepth.Value++;
        
        try
        {
            var binding = new Binding(path)
            {
                Mode = BindingMode.OneWay, // Use OneWay by default to prevent feedback loops
                FallbackValue = GetFallbackValue(path),
                TargetNullValue = GetFallbackValue(path)
            };
            
            if (converter != null)
            {
                // Wrap the converter in a safe wrapper
                binding.Converter = new SafeConverterWrapper(converter);
            }
            
            return binding;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating DataGrid binding for {path}: {ex.Message}");
            return CreateFallbackBinding(path);
        }
        finally
        {
            _bindingDepth.Value--;
        }
    }
    
    /// <summary>
    /// Create a safe DataGridTextColumn
    /// </summary>
    public static DataGridTextColumn CreateSafeTextColumn(string header, string bindingPath, IValueConverter? converter = null)
    {
        try
        {
            return new DataGridTextColumn
            {
                Header = header,
                Binding = CreateSafeBinding(bindingPath, converter),
                Width = DataGridLength.Auto
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating safe DataGrid column: {ex.Message}");
            return new DataGridTextColumn
            {
                Header = header,
                Binding = CreateFallbackBinding(bindingPath),
                Width = DataGridLength.Auto
            };
        }
    }
    
    /// <summary>
    /// Create a fallback binding that won't cause recursion
    /// </summary>
    private static Binding CreateFallbackBinding(string path)
    {
        return new Binding(path)
        {
            Mode = BindingMode.OneTime, // OneTime to prevent any updates
            FallbackValue = GetFallbackValue(path),
            TargetNullValue = GetFallbackValue(path)
        };
    }
    
    /// <summary>
    /// Get a fallback value based on the binding path
    /// </summary>
    private static object GetFallbackValue(string path)
    {
        if (path.Contains("Id") || path.Contains("Count"))
            return 0;
        if (path.Contains("Date") || path.Contains("Time"))
            return DateTime.Now.ToString("yyyy-MM-dd");
        if (path.Contains("Price") || path.Contains("Amount"))
            return "0.00";
        
        return "N/A";
    }
}

/// <summary>
/// Wrapper for value converters that adds safety protection
/// </summary>
internal class SafeConverterWrapper : IValueConverter
{
    private readonly IValueConverter _innerConverter;
    private static readonly ThreadLocal<int> _conversionDepth = new(() => 0);
    private const int MAX_CONVERSION_DEPTH = 2;
    
    public SafeConverterWrapper(IValueConverter innerConverter)
    {
        _innerConverter = innerConverter;
    }
    
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"Converter wrapper blocked due to depth limit: {_innerConverter.GetType().Name}");
            return value?.ToString() ?? "";
        }
        
        _conversionDepth.Value++;
        
        try
        {
            return _innerConverter.Convert(value, targetType, parameter, culture);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in converter wrapper: {_innerConverter.GetType().Name}");
            return value?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in converter wrapper {_innerConverter.GetType().Name}: {ex.Message}");
            return value?.ToString() ?? "";
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"ConvertBack wrapper blocked due to depth limit: {_innerConverter.GetType().Name}");
            return value;
        }
        
        _conversionDepth.Value++;
        
        try
        {
            return _innerConverter.ConvertBack(value, targetType, parameter, culture);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in ConvertBack wrapper: {_innerConverter.GetType().Name}");
            return value;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ConvertBack wrapper {_innerConverter.GetType().Name}: {ex.Message}");
            return value;
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
}
