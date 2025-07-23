using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;

namespace SuperNova.Converters;

/// <summary>
/// Protection system for CultureInfo operations to prevent stack overflow
/// </summary>
public static class CultureInfoProtection
{
    private static readonly ThreadLocal<int> _formatDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, DateTime> _lastFormatTime = new();
    private static readonly ConcurrentDictionary<Type, IFormatProvider> _formatProviderCache = new();
    private static readonly TimeSpan _minFormatInterval = TimeSpan.FromMilliseconds(1);
    
    private const int MAX_FORMAT_DEPTH = 3;
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    private static CultureInfo? _safeCulture = null;
    
    /// <summary>
    /// Initialize the CultureInfo protection system
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
                // Create a safe culture that won't cause recursion
                CreateSafeCulture();
                
                // Pre-cache format providers for common types
                PreCacheFormatProviders();
                
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("CultureInfoProtection initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize CultureInfoProtection: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Get a safe format provider that won't cause recursion
    /// </summary>
    public static IFormatProvider GetSafeFormatProvider(Type formatType)
    {
        var operationKey = $"GetFormat_{formatType?.Name ?? "null"}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check format depth
        if (_formatDepth.Value >= MAX_FORMAT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"Format provider lookup blocked due to depth limit: {formatType?.Name}");
            return GetFallbackFormatProvider(formatType);
        }
        
        // Check format frequency
        var now = DateTime.UtcNow;
        if (_lastFormatTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minFormatInterval)
            {
                System.Diagnostics.Debug.WriteLine($"Format provider lookup blocked due to frequency limit: {formatType?.Name}");
                return _formatProviderCache.GetOrAdd(formatType ?? typeof(object), GetFallbackFormatProvider);
            }
        }
        
        _lastFormatTime[operationKey] = now;
        
        // Try to get from cache first
        if (formatType != null && _formatProviderCache.TryGetValue(formatType, out var cachedProvider))
        {
            return cachedProvider;
        }
        
        _formatDepth.Value++;
        
        try
        {
            var provider = CreateSafeFormatProvider(formatType);
            if (formatType != null)
            {
                _formatProviderCache[formatType] = provider;
            }
            return provider;
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in GetSafeFormatProvider for {formatType?.Name}");
            var fallback = GetFallbackFormatProvider(formatType);
            if (formatType != null)
            {
                _formatProviderCache[formatType] = fallback;
            }
            return fallback;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in GetSafeFormatProvider for {formatType?.Name}: {ex.Message}");
            var fallback = GetFallbackFormatProvider(formatType);
            if (formatType != null)
            {
                _formatProviderCache[formatType] = fallback;
            }
            return fallback;
        }
        finally
        {
            _formatDepth.Value--;
        }
    }
    
    /// <summary>
    /// Get the safe culture instance - CRITICAL FIX: Always return InvariantCulture
    /// </summary>
    public static CultureInfo GetSafeCulture()
    {
        // CRITICAL FIX: Always return InvariantCulture to avoid any custom culture recursion
        return CultureInfo.InvariantCulture;
    }
    
    /// <summary>
    /// Safe string formatting that prevents recursion
    /// </summary>
    public static string SafeFormat(string format, params object[] args)
    {
        var operationKey = $"SafeFormat_{format?.GetHashCode()}_{Thread.CurrentThread.ManagedThreadId}";
        
        // Check format depth
        if (_formatDepth.Value >= MAX_FORMAT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"String formatting blocked due to depth limit: {format}");
            return GetFallbackFormattedString(args);
        }
        
        // Check format frequency
        var now = DateTime.UtcNow;
        if (_lastFormatTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minFormatInterval)
            {
                System.Diagnostics.Debug.WriteLine($"String formatting blocked due to frequency limit: {format}");
                return GetFallbackFormattedString(args);
            }
        }
        
        _lastFormatTime[operationKey] = now;
        _formatDepth.Value++;
        
        try
        {
            if (string.IsNullOrEmpty(format))
                return GetFallbackFormattedString(args);
                
            // Use safe culture for formatting
            return string.Format(GetSafeCulture(), format, args);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in SafeFormat: {format}");
            return GetFallbackFormattedString(args);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeFormat {format}: {ex.Message}");
            return GetFallbackFormattedString(args);
        }
        finally
        {
            _formatDepth.Value--;
        }
    }
    
    /// <summary>
    /// Create a safe culture that won't cause recursion
    /// </summary>
    private static void CreateSafeCulture()
    {
        try
        {
            // Create a minimal culture based on InvariantCulture
            _safeCulture = new SafeCultureInfo();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create safe culture: {ex.Message}");
            _safeCulture = CultureInfo.InvariantCulture;
        }
    }
    
    /// <summary>
    /// Create a safe format provider for the specified type
    /// </summary>
    private static IFormatProvider CreateSafeFormatProvider(Type? formatType)
    {
        if (formatType == null)
            return new SafeFormatProvider();

        if (formatType == typeof(NumberFormatInfo))
            return CultureInfo.InvariantCulture;
        if (formatType == typeof(DateTimeFormatInfo))
            return CultureInfo.InvariantCulture;

        return new SafeFormatProvider();
    }
    
    /// <summary>
    /// Get a fallback format provider that won't cause recursion
    /// </summary>
    private static IFormatProvider GetFallbackFormatProvider(Type? formatType)
    {
        return new SafeFormatProvider();
    }
    
    /// <summary>
    /// Pre-cache format providers for common types
    /// </summary>
    private static void PreCacheFormatProviders()
    {
        var commonTypes = new[]
        {
            typeof(NumberFormatInfo),
            typeof(DateTimeFormatInfo),
            typeof(IFormatProvider),
            typeof(object)
        };
        
        foreach (var type in commonTypes)
        {
            try
            {
                var provider = CreateSafeFormatProvider(type);
                _formatProviderCache[type] = provider;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to pre-cache format provider for {type.Name}: {ex.Message}");
                _formatProviderCache[type] = new SafeFormatProvider();
            }
        }
    }
    
    /// <summary>
    /// Get a fallback formatted string when formatting fails
    /// </summary>
    private static string GetFallbackFormattedString(object[] args)
    {
        try
        {
            if (args == null || args.Length == 0)
                return "";
                
            var result = "";
            for (int i = 0; i < args.Length && i < 5; i++) // Limit to 5 args to prevent issues
            {
                if (i > 0) result += " ";
                result += args[i]?.ToString() ?? "null";
            }
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
        _formatProviderCache.Clear();
    }
}

/// <summary>
/// Safe CultureInfo implementation that prevents recursion
/// </summary>
internal class SafeCultureInfo : CultureInfo
{
    public SafeCultureInfo() : base(CultureInfo.InvariantCulture.Name, false)
    {
        try
        {
            // Ensure we're using the safest possible initialization
            this.ClearCachedData();
        }
        catch
        {
            // Ignore any initialization errors
        }
    }
    
    public override object? GetFormat(Type? formatType)
    {
        try
        {
            // CRITICAL FIX: Use InvariantCulture directly to avoid recursion
            if (formatType == typeof(NumberFormatInfo))
                return CultureInfo.InvariantCulture.NumberFormat;
            if (formatType == typeof(DateTimeFormatInfo))
                return CultureInfo.InvariantCulture.DateTimeFormat;

            return null; // Don't call base to avoid recursion
        }
        catch
        {
            // Return null to prevent any further recursion
            return null;
        }
    }
}

/// <summary>
/// Safe format provider that prevents recursion
/// </summary>
internal class SafeFormatProvider : IFormatProvider
{
    public object? GetFormat(Type? formatType)
    {
        try
        {
            // Return actual format info objects to prevent casting issues
            if (formatType == typeof(NumberFormatInfo))
                return CultureInfo.InvariantCulture.NumberFormat;
            if (formatType == typeof(DateTimeFormatInfo))
                return CultureInfo.InvariantCulture.DateTimeFormat;

            return null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Safe NumberFormatInfo that prevents recursion
/// </summary>
internal class SafeNumberFormatInfo : IFormatProvider
{
    private readonly NumberFormatInfo _inner;

    public SafeNumberFormatInfo()
    {
        try
        {
            // Create a safe copy of InvariantCulture's NumberFormatInfo
            _inner = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        }
        catch
        {
            _inner = CultureInfo.InvariantCulture.NumberFormat;
        }
    }

    public object? GetFormat(Type? formatType)
    {
        if (formatType == typeof(NumberFormatInfo))
            return _inner;
        return null;
    }
}

/// <summary>
/// Safe DateTimeFormatInfo that prevents recursion
/// </summary>
internal class SafeDateTimeFormatInfo : IFormatProvider
{
    private readonly DateTimeFormatInfo _inner;

    public SafeDateTimeFormatInfo()
    {
        try
        {
            // Create a safe copy of InvariantCulture's DateTimeFormatInfo
            _inner = (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();
        }
        catch
        {
            _inner = CultureInfo.InvariantCulture.DateTimeFormat;
        }
    }

    public object? GetFormat(Type? formatType)
    {
        if (formatType == typeof(DateTimeFormatInfo))
            return _inner;
        return null;
    }
}
