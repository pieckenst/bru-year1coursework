using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using Avalonia;
using Avalonia.Data.Converters;

namespace SuperNova.Converters;

/// <summary>
/// КРИТИЧЕСКИЙ КОНВЕРТЕР: Полностью заменяет все DateTime операции безопасными
/// Предотвращает stack overflow в System.DateTimeFormat.FormatCustomized
/// </summary>
public class DateTimeKillerConverter : IValueConverter
{
    private static readonly ThreadLocal<int> _conversionDepth = new(() => 0);
    private static readonly ConcurrentDictionary<string, string> _formatCache = new(); // Composite key: ticks|format|culture|thread
    private static readonly ConcurrentDictionary<string, DateTime> _lastConversionTime = new();
    private static readonly TimeSpan _minConversionInterval = TimeSpan.FromMilliseconds(50);
    private static readonly object _cacheLock = new object();
    private static volatile bool _globalPanic = false;
    private static int _overflowCount = 0;
    private const int PANIC_THRESHOLD = 10;
    private const int MAX_CONVERSION_DEPTH = 1;
    private const int MAX_CACHE_SIZE = 1000;
    private static readonly string[] _whitelistedFormats = new[] { "O", "s", "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-dd" }; // ISO + fallback
    private static readonly string SAFE_DATETIME_STRING = "SAFE_DATETIME";
    private static DateTime _lastCacheCleanup = DateTime.UtcNow;
    private static readonly TimeSpan _cacheCleanupInterval = TimeSpan.FromMinutes(5);
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // КРИТИЧНО: Блокируем любую рекурсию
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine("DateTime conversion blocked due to depth limit");
            return GetSafeDefaultString(value);
        }
        
        var conversionKey = $"Convert_{value?.GetHashCode()}_{Thread.CurrentThread.ManagedThreadId}";
        
        // КРИТИЧНО: Блокируем частые конверсии
        var now = DateTime.UtcNow;
        if (_lastConversionTime.TryGetValue(conversionKey, out var lastTime))
        {
            if (now - lastTime < _minConversionInterval)
            {
                System.Diagnostics.Debug.WriteLine("DateTime conversion blocked due to frequency limit");
                return GetSafeDefaultString(value);
            }
        }
        
        _lastConversionTime[conversionKey] = now;
        _conversionDepth.Value++;
        
        try
        {
            return PerformSafeConversion(value, targetType, parameter);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine("Stack overflow prevented in DateTimeKillerConverter");
            return GetSafeDefaultString(value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in DateTimeKillerConverter: {ex.Message}");
            return GetSafeDefaultString(value);
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // КРИТИЧНО: ConvertBack тоже может вызвать рекурсию
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine("DateTime ConvertBack blocked due to depth limit");
            return GetSafeDefaultValue(targetType);
        }
        
        _conversionDepth.Value++;
        
        try
        {
            return PerformSafeConvertBack(value, targetType);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine("Stack overflow prevented in DateTimeKillerConverter ConvertBack");
            return GetSafeDefaultValue(targetType);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in DateTimeKillerConverter ConvertBack: {ex.Message}");
            return GetSafeDefaultValue(targetType);
        }
        finally
        {
            _conversionDepth.Value--;
        }
    }
    
    /// <summary>
    /// БЕЗОПАСНАЯ конверсия без использования культуры и форматирования
    /// </summary>
    private object? PerformSafeConversion(object? value, Type targetType, object? parameter)
    {
        if (value == null)
            return GetSafeDefaultString(null);
            
        // КРИТИЧНО: Обрабатываем DateTime без форматирования
        if (value is DateTime dt)
        {
            return GetSafeDateTimeString(dt);
        }
        
        if (value is DateTimeOffset dto)
        {
            return GetSafeDateTimeString(dto.DateTime);
        }
        
        // Для других типов - простое преобразование
        if (targetType == typeof(string))
        {
            return GetSafeStringValue(value);
        }
        
        return value;
    }
    
    /// <summary>
    /// БЕЗОПАСНАЯ обратная конверсия
    /// </summary>
    private object? PerformSafeConvertBack(object? value, Type targetType)
    {
        if (value == null)
            return GetSafeDefaultValue(targetType);

        // For TextBox two-way binding, only parse if string is a complete date
        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
        {
            if (value is string str && str.Length == 10 && DateTime.TryParse(str, out var result))
                return result;
            // Prevent binding loop/stack overflow
            return AvaloniaProperty.UnsetValue;
        }

        if (targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset?))
        {
            if (value is string str && str.Length == 10 && DateTimeOffset.TryParse(str, out var result))
                return result;
            return AvaloniaProperty.UnsetValue;
        }

        return GetSafeDefaultValue(targetType);
    }
    
    /// <summary>
    /// БЕЗОПАСНОЕ получение строки DateTime БЕЗ форматирования
    /// </summary>
    private string GetSafeDateTimeString(DateTime dateTime)
    {
        try
        {
            if (_globalPanic)
            {
                System.Diagnostics.Debug.WriteLine("PANIC: Global DateTimeKillerConverter failure state");
                return SAFE_DATETIME_STRING;
            }
            // Composite cache key: ticks|format|culture|thread
            string format = _whitelistedFormats[0]; // Always use first whitelisted format (ISO 8601)
            string culture = CultureInfo.InvariantCulture.Name;
            int threadId = Thread.CurrentThread.ManagedThreadId;
            string cacheKey = $"{dateTime.Ticks}|{format}|{culture}|{threadId}";
            // Periodic cache cleanup
            if ((DateTime.UtcNow - _lastCacheCleanup) > _cacheCleanupInterval)
            {
                lock (_cacheLock)
                {
                    if (_formatCache.Count > MAX_CACHE_SIZE)
                    {
                        _formatCache.Clear();
                        System.Diagnostics.Debug.WriteLine("DateTimeKillerConverter: Cache cleared due to size");
                    }
                    _lastCacheCleanup = DateTime.UtcNow;
                }
            }
            if (_formatCache.TryGetValue(cacheKey, out var cached))
                return cached;
                
            // Format using only whitelisted formats, fallback to SAFE_DATETIME_STRING
            string formatted = SAFE_DATETIME_STRING;
            foreach (var fmt in _whitelistedFormats)
            {
                try
                {
                    formatted = dateTime.ToString(fmt, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(formatted))
                        break;
                }
                catch (Exception fmtEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Format '{fmt}' failed: {fmtEx.Message}");
                }
            }
            if (string.IsNullOrWhiteSpace(formatted))
                formatted = SAFE_DATETIME_STRING;
            try
            {
                if (_formatCache.Count < MAX_CACHE_SIZE)
                    _formatCache[cacheKey] = formatted;
            }
            catch (Exception cacheEx)
            {
                System.Diagnostics.Debug.WriteLine($"Cache add failed: {cacheEx.Message}");
            }
            return formatted;
        }
        catch
        {
            return "DateTime Error";
        }
    }
    
    /// <summary>
    /// БЕЗОПАСНОЕ получение строкового значения
    /// </summary>
    private string GetSafeStringValue(object? value)
    {
        try
        {
            if (value == null) return "";
            if (value is string str) return str;
            if (value is int intVal) return intVal.ToString();
            if (value is double dblVal) return dblVal.ToString("F2");
            if (value is decimal decVal) return decVal.ToString("F2");
            if (value is bool boolVal) return boolVal ? "True" : "False";
            
            // Для других типов - простое имя типа
            return value.GetType().Name;
        }
        catch
        {
            return "Value Error";
        }
    }
    
    /// <summary>
    /// БЕЗОПАСНОЕ значение по умолчанию для строк
    /// </summary>
    private string GetSafeDefaultString(object? value)
    {
        if (value is DateTime) return "DateTime";
        if (value is DateTimeOffset) return "DateTimeOffset";
        return "";
    }
    
    /// <summary>
    /// БЕЗОПАСНОЕ значение по умолчанию для типов
    /// </summary>
    private object? GetSafeDefaultValue(Type targetType)
    {
        try
        {
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return DateTime.Now;
            if (targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset?))
                return DateTimeOffset.Now;
            if (targetType == typeof(string))
                return "";
            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Очистка кэша
    /// </summary>
    public static void ClearCache()
    {
        _formatCache.Clear();
        _lastConversionTime.Clear();
    }
}
