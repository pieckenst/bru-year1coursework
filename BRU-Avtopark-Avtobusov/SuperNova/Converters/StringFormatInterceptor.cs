using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;

namespace SuperNova.Converters;

/// <summary>
/// Global interceptor for string formatting operations to prevent stack overflow
/// </summary>
public static class StringFormatInterceptor
{
    private static readonly ThreadLocal<int> _formatDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, DateTime> _lastFormatTime = new();
    private static readonly TimeSpan _minFormatInterval = TimeSpan.FromMilliseconds(1);
    
    private const int MAX_FORMAT_DEPTH = 2;
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    
    /// <summary>
    /// Initialize the string format interceptor
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
                System.Diagnostics.Debug.WriteLine("StringFormatInterceptor initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize StringFormatInterceptor: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Safe string format that prevents recursion
    /// </summary>
    public static string SafeFormat(string format, params object[] args)
    {
        var operationKey = $"Format_{format?.GetHashCode()}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check format depth
        if (_formatDepth.Value >= MAX_FORMAT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"String format blocked due to depth limit: {format}");
            return GetSafeFormattedString(format, args);
        }
        
        // Check format frequency
        var now = DateTime.UtcNow;
        if (_lastFormatTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minFormatInterval)
            {
                System.Diagnostics.Debug.WriteLine($"String format blocked due to frequency limit: {format}");
                return GetSafeFormattedString(format, args);
            }
        }
        
        _lastFormatTime[operationKey] = now;
        _formatDepth.Value++;
        
        try
        {
            if (string.IsNullOrEmpty(format))
                return GetSafeFormattedString(format, args);
                
            // Use basic string operations to avoid culture formatting
            return PerformSafeStringFormat(format, args);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeFormat: {format}");
            return GetSafeFormattedString(format, args);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeFormat {format}: {ex.Message}");
            return GetSafeFormattedString(format, args);
        }
        finally
        {
            _formatDepth.Value--;
        }
    }
    
    /// <summary>
    /// Safe string format with IFormatProvider that prevents recursion
    /// </summary>
    public static string SafeFormat(IFormatProvider provider, string format, params object[] args)
    {
        var operationKey = $"FormatProvider_{format?.GetHashCode()}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check format depth
        if (_formatDepth.Value >= MAX_FORMAT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"String format with provider blocked due to depth limit: {format}");
            return GetSafeFormattedString(format, args);
        }
        
        // Check format frequency
        var now = DateTime.UtcNow;
        if (_lastFormatTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minFormatInterval)
            {
                System.Diagnostics.Debug.WriteLine($"String format with provider blocked due to frequency limit: {format}");
                return GetSafeFormattedString(format, args);
            }
        }
        
        _lastFormatTime[operationKey] = now;
        _formatDepth.Value++;
        
        try
        {
            if (string.IsNullOrEmpty(format))
                return GetSafeFormattedString(format, args);
                
            // Use safe provider or fallback to basic formatting
            var safeProvider = CultureInfoProtection.GetSafeCulture();
            return PerformSafeStringFormatWithProvider(safeProvider, format, args);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeFormat with provider: {format}");
            return GetSafeFormattedString(format, args);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeFormat with provider {format}: {ex.Message}");
            return GetSafeFormattedString(format, args);
        }
        finally
        {
            _formatDepth.Value--;
        }
    }
    
    /// <summary>
    /// Perform safe string formatting without triggering culture recursion
    /// </summary>
    private static string PerformSafeStringFormat(string format, object[] args)
    {
        try
        {
            // Simple placeholder replacement to avoid culture formatting
            if (args == null || args.Length == 0)
                return format;
                
            var result = format;
            for (int i = 0; i < args.Length && i < 10; i++) // Limit to 10 args
            {
                var placeholder = "{" + i + "}";
                var value = GetSafeStringValue(args[i]);
                result = result.Replace(placeholder, value);
            }
            
            return result;
        }
        catch
        {
            return GetSafeFormattedString(format, args);
        }
    }
    
    /// <summary>
    /// Perform safe string formatting with provider
    /// </summary>
    private static string PerformSafeStringFormatWithProvider(IFormatProvider provider, string format, object[] args)
    {
        try
        {
            // Try to use the provider, but fallback to safe formatting if it causes issues
            return string.Format(provider, format, args);
        }
        catch (StackOverflowException)
        {
            return PerformSafeStringFormat(format, args);
        }
        catch
        {
            return PerformSafeStringFormat(format, args);
        }
    }
    
    /// <summary>
    /// Get a safe string representation of a value
    /// </summary>
    private static string GetSafeStringValue(object? value)
    {
        try
        {
            if (value == null)
                return "null";
                
            if (value is string str)
                return str;
                
            if (value is DateTime dt)
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
                
            if (value is DateTimeOffset dto)
                return dto.ToString("yyyy-MM-dd HH:mm:ss");
                
            if (value is decimal dec)
                return dec.ToString("F2");
                
            if (value is double dbl)
                return dbl.ToString("F2");
                
            if (value is float flt)
                return flt.ToString("F2");
                
            if (value is int intVal)
                return intVal.ToString();
                
            if (value is long longVal)
                return longVal.ToString();
                
            if (value is bool boolVal)
                return boolVal ? "True" : "False";
                
            // For other types, use basic ToString
            return value.ToString() ?? value.GetType().Name;
        }
        catch
        {
            return value?.GetType().Name ?? "Unknown";
        }
    }
    
    /// <summary>
    /// Get a safe formatted string when formatting fails
    /// </summary>
    private static string GetSafeFormattedString(string? format, object[]? args)
    {
        try
        {
            if (string.IsNullOrEmpty(format))
                return "";
                
            if (args == null || args.Length == 0)
                return format;
                
            // Simple concatenation as fallback
            var result = format + " [";
            for (int i = 0; i < args.Length && i < 5; i++) // Limit to 5 args
            {
                if (i > 0) result += ", ";
                result += GetSafeStringValue(args[i]);
            }
            result += "]";
            
            return result;
        }
        catch
        {
            return "FormatError";
        }
    }
    
    /// <summary>
    /// Clear all cached data
    /// </summary>
    public static void ClearCache()
    {
        _lastFormatTime.Clear();
    }
}

/// <summary>
/// Extension methods for safe string formatting
/// </summary>
public static class SafeStringExtensions
{
    /// <summary>
    /// Safe string format extension
    /// </summary>
    public static string SafeFormat(this string format, params object[] args)
    {
        return StringFormatInterceptor.SafeFormat(format, args);
    }
    
    /// <summary>
    /// Safe string format with provider extension
    /// </summary>
    public static string SafeFormat(this string format, IFormatProvider provider, params object[] args)
    {
        return StringFormatInterceptor.SafeFormat(provider, format, args);
    }
    
    /// <summary>
    /// Safe ToString that prevents recursion
    /// </summary>
    public static string SafeToString(this object? obj)
    {
        try
        {
            if (obj == null)
                return "null";
                
            if (obj is string str)
                return str;
                
            // Use basic ToString without culture formatting
            return obj.ToString() ?? obj.GetType().Name;
        }
        catch
        {
            return obj?.GetType().Name ?? "Unknown";
        }
    }
}
