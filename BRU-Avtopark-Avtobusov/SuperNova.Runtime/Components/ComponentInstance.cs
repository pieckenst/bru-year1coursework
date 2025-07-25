using System;
using System.Collections.Generic;
using Avalonia.Data;
using static SuperNova.Runtime.Components.VBProperties;

namespace SuperNova.Runtime.Components;

public partial class ComponentInstance
{
    public ComponentBaseClass BaseClass { get; }

    public event Action<ComponentInstance, PropertyClass, object?, object?> OnComponentPropertyChanging = delegate { };
    public event Action<ComponentInstance, PropertyClass> OnComponentPropertyChanged = delegate { };

    private Dictionary<PropertyClass, object?> properties { get; } = new();

    public ComponentInstance(ComponentBaseClass baseClass, string name)
    {
        BaseClass = baseClass;
        SetProperty(NameProperty, name);
    }

    public ComponentInstance SetProperty<T>(PropertyClass<T> propertyClass, T? value)
    {
        // Defensive guard: Prevent DateTime or other non-string assignment to string properties
        if (propertyClass.PropertyType == typeof(string) && value != null && value is not string)
        {
            // Log the type mismatch
            System.Diagnostics.Debug.WriteLine($"[DEFENSIVE GUARD] Attempted to assign value of type {value.GetType().Name} to string property '{propertyClass.Name}' on {BaseClass.Name}. Value: {value}");
            // Optionally, you could throw or ignore the assignment
            return this;
        }
        var oldValue = properties.GetValueOrDefault(propertyClass, UnsetValue.Instance);
        OnComponentPropertyChanging?.Invoke(this, propertyClass, oldValue, value);
        properties[propertyClass] = value;
        OnComponentPropertyChanged?.Invoke(this, propertyClass);
        return this;
    }

    public void SetUntypedProperty(PropertyClass propertyClass, object? untypedValue)
    {
        // Defensive guard: Prevent DateTime or other non-string assignment to string properties
        if (propertyClass.PropertyType == typeof(string) && untypedValue != null && untypedValue is not string)
        {
            System.Diagnostics.Debug.WriteLine($"[DEFENSIVE GUARD] Attempted to assign value of type {untypedValue.GetType().Name} to string property '{propertyClass.Name}' on {BaseClass.Name}. Value: {untypedValue}");
            return;
        }
        if (untypedValue == null || propertyClass.PropertyType.IsInstanceOfType(untypedValue))
        {
            if (ReferenceEquals(propertyClass, NameProperty) &&
                string.IsNullOrEmpty(untypedValue as string))
                throw new DataValidationException("Name can't be empty");
            var oldValue = properties.GetValueOrDefault(propertyClass, UnsetValue.Instance);
            OnComponentPropertyChanging?.Invoke(this, propertyClass, oldValue, untypedValue);
            properties[propertyClass] = untypedValue;
            OnComponentPropertyChanged?.Invoke(this, propertyClass);
        }
    }

    public object? GetBoxedPropertyOrDefault(PropertyClass property)
    {
        if (properties.TryGetValue(property, out var result))
        {
            return result;
        }

        return property.BoxedDefaultValue(BaseClass);
    }

    public T? GetPropertyOrDefault<T>(PropertyClass<T> property)
    {
        if (TryGetProperty<T>(property, out var result))
            return result;
        return property.DefaultValue(BaseClass);
    }

    public bool TryGetProperty<T>(PropertyClass<T> property, out T value)
    {
        if (properties.TryGetValue(property, out var result))
        {
            if (result == null)
            {
                value = default!;
                return true;
            }

            if (result is T t)
            {
                value = t;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public bool TryGetBoxedProperty(PropertyClass property, out object? value)
    {
        if (properties.TryGetValue(property, out var result))
        {
            value = result;
            return true;
        }

        value = default!;
        return false;
    }

    public int this[PropertyClass<int> property] => GetPropertyOrDefault(property);
    public double this[PropertyClass<double> property] => GetPropertyOrDefault(property);
    public string? this[PropertyClass<string> property] => GetPropertyOrDefault(property);
}