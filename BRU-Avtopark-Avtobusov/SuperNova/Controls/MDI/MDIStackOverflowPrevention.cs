using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SuperNova.Controls;

/// <summary>
/// Comprehensive stack overflow prevention system for MDI operations
/// </summary>
public static class MDIStackOverflowPrevention
{
    private static readonly ThreadLocal<int> _operationDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, DateTime> _lastOperationTime = new();
    private static readonly TimeSpan _minOperationInterval = TimeSpan.FromMilliseconds(10);
    
    private const int MAX_OPERATION_DEPTH = 5;
    private const int MAX_OPERATIONS_PER_SECOND = 100;
    
    /// <summary>
    /// Check if an MDI operation can proceed safely
    /// </summary>
    public static bool CanProceed(string operationName)
    {
        // Check recursion depth
        if (_operationDepth.Value >= MAX_OPERATION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine($"MDI operation '{operationName}' blocked due to recursion depth limit ({_operationDepth.Value})");
            return false;
        }
        
        // Check operation frequency
        var now = DateTime.UtcNow;
        if (_lastOperationTime.TryGetValue(operationName, out var lastTime))
        {
            if (now - lastTime < _minOperationInterval)
            {
                System.Diagnostics.Debug.WriteLine($"MDI operation '{operationName}' blocked due to frequency limit");
                return false;
            }
        }
        
        _lastOperationTime[operationName] = now;
        return true;
    }
    
    /// <summary>
    /// Enter a protected MDI operation
    /// </summary>
    public static IDisposable EnterOperation(string operationName)
    {
        if (!CanProceed(operationName))
            return new NoOpDisposable();
            
        _operationDepth.Value++;
        return new OperationGuard(operationName);
    }
    
    /// <summary>
    /// Execute an MDI operation safely with automatic protection
    /// </summary>
    public static void SafeExecute(string operationName, Action operation)
    {
        using var guard = EnterOperation(operationName);
        if (guard is NoOpDisposable)
            return;
            
        try
        {
            operation();
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in MDI operation '{operationName}'");
            // Don't rethrow stack overflow exceptions
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MDI operation '{operationName}': {ex.Message}");
            // Log but don't crash the application
        }
    }
    
    /// <summary>
    /// Execute an MDI operation safely with return value
    /// </summary>
    public static T SafeExecute<T>(string operationName, Func<T> operation, T defaultValue = default(T))
    {
        using var guard = EnterOperation(operationName);
        if (guard is NoOpDisposable)
            return defaultValue;
            
        try
        {
            return operation();
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"Stack overflow prevented in MDI operation '{operationName}'");
            return defaultValue;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in MDI operation '{operationName}': {ex.Message}");
            return defaultValue;
        }
    }
    
    private class OperationGuard : IDisposable
    {
        private readonly string _operationName;
        private bool _disposed = false;
        
        public OperationGuard(string operationName)
        {
            _operationName = operationName;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_operationDepth.Value > 0)
                    _operationDepth.Value--;
                _disposed = true;
            }
        }
    }
    
    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
