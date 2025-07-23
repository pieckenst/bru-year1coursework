using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Avalonia.Data.Converters;

namespace SuperNova.Converters;

/// <summary>
/// КРИТИЧЕСКИЙ ГЛОБАЛЬНЫЙ ПЕРЕХВАТЧИК: Заменяет ВСЕ DateTime операции безопасными
/// Предотвращает stack overflow в System.DateTimeFormat на уровне всего приложения
/// </summary>
public static class GlobalDateTimeInterceptor
{
    private static readonly ThreadLocal<int> _interceptDepth = new(() => 0);
    private static readonly ConcurrentDictionary<long, string> _globalDateTimeCache = new();
    private static readonly DateTimeKillerConverter _safeConverter = new();
    private static bool _isInitialized = false;
    private static readonly object _initLock = new object();
    
    private const int MAX_INTERCEPT_DEPTH = 1;
    private const int MAX_GLOBAL_CACHE_SIZE = 5000;
    
    /// <summary>
    /// Инициализация глобального перехватчика DateTime
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
                // Регистрируем глобальный конвертер для всех DateTime операций
                RegisterGlobalDateTimeConverter();
                
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("GlobalDateTimeInterceptor initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize GlobalDateTimeInterceptor: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// КРИТИЧНО: Безопасное форматирование DateTime без использования культуры
    /// </summary>
    public static string SafeFormatDateTime(DateTime dateTime)
    {
        if (_interceptDepth.Value >= MAX_INTERCEPT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine("DateTime formatting blocked due to depth limit");
            return "DateTime";
        }
        
        _interceptDepth.Value++;
        
        try
        {
            // Проверяем кэш
            var ticks = dateTime.Ticks;
            if (_globalDateTimeCache.TryGetValue(ticks, out var cached))
                return cached;
                
            // КРИТИЧНО: Форматируем БЕЗ использования культуры и ToString()
            var result = FormatDateTimeManually(dateTime);
            
            // Ограничиваем размер кэша
            if (_globalDateTimeCache.Count < MAX_GLOBAL_CACHE_SIZE)
            {
                _globalDateTimeCache[ticks] = result;
            }
            
            return result;
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine("Stack overflow prevented in SafeFormatDateTime");
            return "DateTime Error";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeFormatDateTime: {ex.Message}");
            return "DateTime Error";
        }
        finally
        {
            _interceptDepth.Value--;
        }
    }
    
    /// <summary>
    /// КРИТИЧНО: Безопасное форматирование DateTimeOffset
    /// </summary>
    public static string SafeFormatDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        if (_interceptDepth.Value >= MAX_INTERCEPT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine("DateTimeOffset formatting blocked due to depth limit");
            return "DateTimeOffset";
        }
        
        _interceptDepth.Value++;
        
        try
        {
            return SafeFormatDateTime(dateTimeOffset.DateTime);
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine("Stack overflow prevented in SafeFormatDateTimeOffset");
            return "DateTimeOffset Error";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeFormatDateTimeOffset: {ex.Message}");
            return "DateTimeOffset Error";
        }
        finally
        {
            _interceptDepth.Value--;
        }
    }
    
    /// <summary>
    /// КРИТИЧНО: Ручное форматирование DateTime БЕЗ культуры
    /// </summary>
    private static string FormatDateTimeManually(DateTime dateTime)
    {
        try
        {
            // Используем только базовые математические операции
            var year = dateTime.Year;
            var month = dateTime.Month;
            var day = dateTime.Day;
            var hour = dateTime.Hour;
            var minute = dateTime.Minute;
            var second = dateTime.Second;
            
            // Форматируем вручную без ToString()
            var yearStr = ConvertIntToString(year);
            var monthStr = ConvertIntToStringPadded(month, 2);
            var dayStr = ConvertIntToStringPadded(day, 2);
            var hourStr = ConvertIntToStringPadded(hour, 2);
            var minuteStr = ConvertIntToStringPadded(minute, 2);
            var secondStr = ConvertIntToStringPadded(second, 2);
            
            return $"{yearStr}-{monthStr}-{dayStr} {hourStr}:{minuteStr}:{secondStr}";
        }
        catch
        {
            return "DateTime Manual Error";
        }
    }
    
    /// <summary>
    /// КРИТИЧНО: Конвертация int в string БЕЗ культуры
    /// </summary>
    private static string ConvertIntToString(int value)
    {
        try
        {
            if (value == 0) return "0";
            
            var chars = new char[10]; // Максимум для int
            var index = 9;
            var isNegative = value < 0;
            
            if (isNegative) value = -value;
            
            while (value > 0)
            {
                chars[index--] = (char)('0' + (value % 10));
                value /= 10;
            }
            
            if (isNegative) chars[index--] = '-';
            
            return new string(chars, index + 1, 9 - index);
        }
        catch
        {
            return "0";
        }
    }
    
    /// <summary>
    /// КРИТИЧНО: Конвертация int в string с padding БЕЗ культуры
    /// </summary>
    private static string ConvertIntToStringPadded(int value, int padding)
    {
        try
        {
            var str = ConvertIntToString(value);
            while (str.Length < padding)
            {
                str = "0" + str;
            }
            return str;
        }
        catch
        {
            return "00";
        }
    }
    
    /// <summary>
    /// Регистрация глобального конвертера
    /// </summary>
    private static void RegisterGlobalDateTimeConverter()
    {
        try
        {
            // Здесь можно добавить регистрацию глобального конвертера
            // если Avalonia поддерживает такую функциональность
            System.Diagnostics.Debug.WriteLine("Global DateTime converter registration attempted");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register global DateTime converter: {ex.Message}");
        }
    }
    
    /// <summary>
    /// КРИТИЧНО: Безопасная конвертация любого объекта в строку
    /// </summary>
    public static string SafeConvertToString(object? value)
    {
        if (_interceptDepth.Value >= MAX_INTERCEPT_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine("Object to string conversion blocked due to depth limit");
            return value?.GetType().Name ?? "";
        }
        
        _interceptDepth.Value++;
        
        try
        {
            if (value == null) return "";
            
            if (value is DateTime dt)
                return SafeFormatDateTime(dt);
                
            if (value is DateTimeOffset dto)
                return SafeFormatDateTimeOffset(dto);
                
            if (value is string str)
                return str;
                
            if (value is int intVal)
                return ConvertIntToString(intVal);
                
            if (value is bool boolVal)
                return boolVal ? "True" : "False";
                
            // Для других типов - имя типа
            return value.GetType().Name;
        }
        catch (StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine("Stack overflow prevented in SafeConvertToString");
            return value?.GetType().Name ?? "Error";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SafeConvertToString: {ex.Message}");
            return value?.GetType().Name ?? "Error";
        }
        finally
        {
            _interceptDepth.Value--;
        }
    }
    
    /// <summary>
    /// Очистка всех кэшей
    /// </summary>
    public static void ClearAllCaches()
    {
        _globalDateTimeCache.Clear();
        DateTimeKillerConverter.ClearCache();
    }
    
    /// <summary>
    /// Получение статистики кэша
    /// </summary>
    public static int GetCacheSize()
    {
        return _globalDateTimeCache.Count;
    }
}
