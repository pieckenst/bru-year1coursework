using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using DynamicForms.Library.Core.Attributes;
using DynamicForms.Library.Core.Shared;

namespace DynamicForms.Library.Core;

public class DynamicFormField : DynamicFormObject
{
    [JsonConstructor]
    public DynamicFormField()
    {
        try
        {
            // Initialize required non-nullable properties
            ParentObject = null!;
            Attributes = null!;
            FieldId = Guid.NewGuid().ToString();
            FieldName = string.Empty;
            FieldType = DynamicFormFieldType.Text.ToString();
            PropertyName = string.Empty;
            ParentGroupName = string.Empty;
            ValidationRules = string.Empty;
            FieldMetadata = new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DynamicFormField constructor: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    public DynamicFormField(object parent, object? value, PropertyInfo? property, DynamicFormFieldAttribute attribute, string groupName)
    {
        try
        {
            ParentObject = parent;
            Value = value;
            Property = property;
            Attributes = attribute;
            ParentGroupName = groupName;
            
            // Initialize serializable properties
            FieldId = Guid.NewGuid().ToString();
            FieldName = attribute.Label;
            FieldType = attribute.FieldType.ToString();
            IsRequired = !property?.PropertyType.IsNullable() ?? false;
            PropertyName = property?.Name ?? "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DynamicFormField constructor with parameters: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    public override bool IsGroup => false;

    [JsonIgnore]
    public object ParentObject { get; private set; }
    
    [JsonIgnore]
    public object? Value { get; set; }

    [JsonIgnore]
    public PropertyInfo? Property { get; private set; }

    [JsonIgnore]
    public DynamicFormFieldAttribute Attributes { get; private set; }

    public override string ParentGroupName { get; init; }

    [JsonIgnore]
    public DynamicFormFieldType Type => Attributes?.FieldType ?? DynamicFormFieldType.Text;

    public string PropertyName { get; init; }

    // Serializable properties
    public string FieldId { get; init; }
    public string FieldName { get; init; }
    public string FieldType { get; init; }
    public bool IsRequired { get; init; }
    public string ValidationRules { get; set; }
    public Dictionary<string, object> FieldMetadata { get; set; }

    public void SetValue(object? parentObject, object? value)
    {
        try
        {
            Property?.SetValue(parentObject, value);
            Value = value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting value: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    public object? GetValue(object? parentObject)
    {
        try
        {
            return Property?.GetValue(parentObject);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting value: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }
}