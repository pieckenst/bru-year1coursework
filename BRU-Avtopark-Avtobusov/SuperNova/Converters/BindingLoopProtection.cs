using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace SuperNova.Converters;

/// <summary>
/// Global protection system for binding loops and recursive operations
/// </summary>
public static class BindingLoopProtection
{
    private static readonly ThreadLocal<int> _bindingDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, DateTime> _lastBindingTime = new();
    private static readonly ConcurrentDictionary<string, int> _bindingCount = new();
    private static readonly TimeSpan _minBindingInterval = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan _resetInterval = TimeSpan.FromSeconds(1);
    
    private const int MAX_BINDING_DEPTH = 3; // ENHANCED: Reduced from 10 to 3
    private const int MAX_BINDINGS_PER_SECOND = 100; // ENHANCED: Reduced from 1000 to 100
    
    /// <summary>
    /// Check if a binding operation can proceed safely
    /// </summary>
    public static bool CanProceedWithBinding(string operationKey)
    {
        // Check binding depth
        if (_bindingDepth.Value >= MAX_BINDING_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"Binding blocked due to depth limit: {operationKey}");
            return false;
        }
        
        var now = DateTime.UtcNow;
        
        // Check binding frequency
        if (_lastBindingTime.TryGetValue(operationKey, out var lastTime))
        {
            if (now - lastTime < _minBindingInterval)
            {
                System.Diagnostics.Debug.WriteLine($"Binding blocked due to frequency limit: {operationKey}");
                return false;
            }
        }
        
        // Check binding count per second
        var countKey = $"{operationKey}_{now:yyyy-MM-dd-HH-mm-ss}";
        var currentCount = _bindingCount.AddOrUpdate(countKey, 1, (key, count) => count + 1);
        
        if (currentCount > MAX_BINDINGS_PER_SECOND)
        {
            System.Diagnostics.Debug.WriteLine($"Binding blocked due to count limit: {operationKey}");
            return false;
        }
        
        _lastBindingTime[operationKey] = now;
        
        // Clean up old entries
        CleanupOldEntries(now);
        
        return true;
    }
    
    /// <summary>
    /// Enter a protected binding operation
    /// </summary>
    public static IDisposable EnterBindingOperation(string operationKey)
    {
        if (!CanProceedWithBinding(operationKey))
            return new NoOpDisposable();
            
        _bindingDepth.Value++;
        return new BindingOperationGuard();
    }
    
    /// <summary>
    /// Execute a binding operation safely
    /// </summary>
    public static void SafeExecuteBinding(string operationKey, Action operation)
    {
        using var guard = EnterBindingOperation(operationKey);
        if (guard is NoOpDisposable)
            return;
            
        try
        {
            operation();
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in binding operation: {operationKey}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in binding operation {operationKey}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Execute a binding operation safely with return value
    /// </summary>
    public static T SafeExecuteBinding<T>(string operationKey, Func<T> operation, T defaultValue = default(T))
    {
        using var guard = EnterBindingOperation(operationKey);
        if (guard is NoOpDisposable)
            return defaultValue;
            
        try
        {
            return operation();
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in binding operation: {operationKey}");
            return defaultValue;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in binding operation {operationKey}: {ex.Message}");
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Clean up old entries to prevent memory leaks
    /// </summary>
    private static void CleanupOldEntries(DateTime now)
    {
        // Clean up entries older than reset interval
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _lastBindingTime)
        {
            if (now - kvp.Value > _resetInterval)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            _lastBindingTime.TryRemove(key, out _);
        }
        
        // Clean up count entries
        var countKeysToRemove = new List<string>();
        foreach (var kvp in _bindingCount)
        {
            if (kvp.Key.Contains("_"))
            {
                var parts = kvp.Key.Split('_');
                if (parts.Length > 1)
                {
                    var timeStr = parts[parts.Length - 1];
                    if (DateTime.TryParseExact(timeStr, "yyyy-MM-dd-HH-mm-ss", null, System.Globalization.DateTimeStyles.None, out var entryTime))
                    {
                        if (now - entryTime > _resetInterval)
                        {
                            countKeysToRemove.Add(kvp.Key);
                        }
                    }
                }
            }
        }
        
        foreach (var key in countKeysToRemove)
        {
            _bindingCount.TryRemove(key, out _);
        }
    }
    
    /// <summary>
    /// Clear all protection data (useful for testing)
    /// </summary>
    public static void ClearProtectionData()
    {
        _lastBindingTime.Clear();
        _bindingCount.Clear();
    }
    
    private class BindingOperationGuard : IDisposable
    {
        private bool _disposed = false;
        
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_bindingDepth.Value > 0)
                    _bindingDepth.Value--;
                _disposed = true;
            }
        }
    }
    
    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Extension methods for safe binding operations
/// </summary>
public static class BindingExtensions
{
    /// <summary>
    /// Execute a binding operation with automatic protection
    /// </summary>
    public static void SafeExecute(this object source, string operationName, Action operation)
    {
        var operationKey = $"{source.GetType().Name}_{operationName}_{Thread.CurrentThread.ManagedThreadId}";
        BindingLoopProtection.SafeExecuteBinding(operationKey, operation);
    }
    
    /// <summary>
    /// Execute a binding operation with automatic protection and return value
    /// </summary>
    public static T SafeExecute<T>(this object source, string operationName, Func<T> operation, T defaultValue = default(T))
    {
        var operationKey = $"{source.GetType().Name}_{operationName}_{Thread.CurrentThread.ManagedThreadId}";
        return BindingLoopProtection.SafeExecuteBinding(operationKey, operation, defaultValue);
    }
}
