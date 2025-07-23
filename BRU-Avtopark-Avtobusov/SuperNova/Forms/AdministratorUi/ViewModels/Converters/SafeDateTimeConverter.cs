using System;
using System.Globalization;
using Avalonia;
using System.Threading;
using Avalonia.Data.Converters;

namespace SuperNova.Forms.AdministratorUi.ViewModels.Converters;

/// <summary>
/// Safe DateTime converter that prevents binding loops and stack overflow issues
/// </summary>
public class SafeDateTimeConverter : IValueConverter
{
    private static readonly object _lockObject = new object();
    private static volatile bool _isConverting = false;
    private static readonly ThreadLocal<int> _conversionDepth = new(() => 0);
    private static volatile bool _globalPanic = false;
    private static int _overflowCount = 0;
    private const int PANIC_THRESHOLD = 10;
    private const int MAX_CONVERSION_DEPTH = 2;
    private static readonly string SAFE_DATETIME_STRING = "SAFE_DATETIME";
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_globalPanic)
        {
            System.Diagnostics.Debug.WriteLine("PANIC: Global SafeDateTimeConverter failure state");
            return SAFE_DATETIME_STRING;
        }
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine("SafeDateTimeConverter: conversion blocked due to depth limit");
            return SAFE_DATETIME_STRING;
        }
        if (_isConverting)
            return value;
        lock (_lockObject)
        {
            if (_isConverting)
                return value;
            try
            {
                _isConverting = true;
                _conversionDepth.Value++;
                if (value == null)
                    return null;
                if (value is DateTimeOffset dto)
                    return dto.DateTime;
                if (value is DateTimeOffset ndto2)
                    return ndto2.DateTime;
                if (value is DateTime dt)
                    return dt;
                if (value is DateTime ndt2)
                    return ndt2;
                if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    try
                    {
                        if (DateTime.TryParse(stringValue, culture, DateTimeStyles.None, out var result))
                            return result;
                        if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invResult))
                            return invResult;
                    }
                    catch (Exception parseEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"SafeDateTimeConverter: Parse failed: {parseEx.Message}");
                    }
                }
                return value;
            }
            catch (StackOverflowException)
            {
                System.Diagnostics.Debug.WriteLine("SafeDateTimeConverter: Stack overflow prevented");
                _overflowCount++;
                if (_overflowCount > PANIC_THRESHOLD)
                    _globalPanic = true;
                return SAFE_DATETIME_STRING;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeDateTimeConverter: Error: {ex.Message}");
                return null;
            }
            finally
            {
                _isConverting = false;
                _conversionDepth.Value--;
            }
        }
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        System.Diagnostics.Debug.WriteLine($"[SafeDateTimeConverter] ConvertBack called. Value: {value}, TargetType: {targetType}");
        if (_globalPanic)
        {
            System.Diagnostics.Debug.WriteLine("PANIC: Global SafeDateTimeConverter failure state");
            return SAFE_DATETIME_STRING;
        }
        if (_conversionDepth.Value >= MAX_CONVERSION_DEPTH)
        {
            System.Diagnostics.Debug.WriteLine("SafeDateTimeConverter: ConvertBack blocked due to depth limit");
            return SAFE_DATETIME_STRING;
        }
        if (_isConverting)
            return value;
        lock (_lockObject)
        {
            if (_isConverting)
                return value;
            try
            {
                _isConverting = true;
                
                if (value == null)
                    return null;
                    
                // Handle DateTime to DateTimeOffset conversion
                if (value is DateTime dateTime)
                {
                    return new DateTimeOffset(dateTime);
                }
                
                // Handle string conversion
                if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    if (DateTime.TryParse(stringValue, culture, DateTimeStyles.None, out var result))
                    {
                        return new DateTimeOffset(result);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[SafeDateTimeConverter] ConvertBack: Invalid or incomplete date string: '{stringValue}'. Returning AvaloniaProperty.UnsetValue.");
                        return AvaloniaProperty.UnsetValue;
                    }
                }
                
                // Return UnsetValue for non-string/invalid
                System.Diagnostics.Debug.WriteLine($"[SafeDateTimeConverter] ConvertBack: Input is not a valid date string. Returning AvaloniaProperty.UnsetValue.");
                return AvaloniaProperty.UnsetValue;
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine($"[SafeDateTimeConverter] ConvertBack: Error occurred during conversion. Returning null.");
                // Return null on any conversion error to prevent crashes
                return null;
            }
            finally
            {
                _isConverting = false;
            }
        }
    }
}

/// <summary>
/// Static helper methods for safe DateTime operations
/// </summary>
public static class SafeDateTimeHelper
{
    /// <summary>
    /// Safely extract Date component from DateTimeOffset? without causing binding loops
    /// </summary>
    public static DateTime SafeGetDate(DateTimeOffset? dateTimeOffset, DateTime defaultValue = default)
    {
        try
        {
            return dateTimeOffset?.Date ?? defaultValue;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Safely extract DateTime from DateTimeOffset? without causing binding loops
    /// </summary>
    public static DateTime SafeGetDateTime(DateTimeOffset? dateTimeOffset, DateTime defaultValue = default)
    {
        try
        {
            return dateTimeOffset?.DateTime ?? defaultValue;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Safely create DateTimeOffset from DateTime
    /// </summary>
    public static DateTimeOffset SafeCreateDateTimeOffset(DateTime dateTime)
    {
        try
        {
            return new DateTimeOffset(dateTime);
        }
        catch (Exception)
        {
            return DateTimeOffset.Now;
        }
    }
}
