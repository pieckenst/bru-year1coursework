using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicForms.Library.Core.Attributes;
using DynamicForms.Library.Core.Shared;

namespace DynamicForms.Library.Core.Serialization;

public class DynamicFormSerializationContext
{
    public object? ParentObject { get; set; }
    public Dictionary<string, object> PropertyValues { get; } = new();
    public Dictionary<string, List<DynamicFormFieldAttribute>> FieldAttributes { get; } = new();
    public Dictionary<string, List<DynamicFormGroupAttribute>> GroupAttributes { get; } = new();
}

public class DynamicFormSerializationOptions
{
    public bool IncludePropertyValues { get; set; } = true;
    public bool IncludeAttributes { get; set; } = true;
    public DynamicFormPlatform TargetPlatform { get; set; } = DynamicFormPlatform.All;
}

public static class DynamicFormSerializer
{
    public static string Serialize(DynamicForm form, DynamicFormSerializationOptions? options = null)
    {
        Console.WriteLine("Starting serialization of DynamicForm...");

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        try
        {
            var formData = new
            {
                form.FormId,
                form.FormName,
                form.Metadata,
                ParentGroup = new
                {
                    GroupName = form.ParentGroup.GroupName,
                    Style = form.ParentGroup.Style,
                    Type = form.ParentGroup.Type,
                    GroupId = form.ParentGroup.GroupId,
                    GroupMetadata = form.ParentGroup.GroupMetadata,
                    Objects = SerializeObjects(form.ParentGroup.Objects)
                }
            };

            string serializedJson = JsonSerializer.Serialize(formData, jsonOptions);
            Console.WriteLine("DynamicForm serialized successfully.");
            return serializedJson;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during serialization: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    private static List<object> SerializeObjects(List<DynamicFormObject> objects)
    {
        var serializedObjects = new List<object>();

        foreach (var obj in objects)
        {
            try
            {
                if (obj is DynamicFormField field)
                {
                    var fieldData = new
                    {
                        ObjectType = "Field",
                        field.FieldId,
                        field.FieldName,
                        field.FieldType,
                        field.PropertyName,
                        field.ParentGroupName,
                        field.IsRequired,
                        field.ValidationRules,
                        field.FieldMetadata,
                        AttributeType = field.Attributes.GetType().AssemblyQualifiedName,
                        AttributeData = SerializeAttribute(field.Attributes),
                        Value = SerializeValue(field.Value, field.Type)
                    };
                    serializedObjects.Add(fieldData);
                }
                else if (obj is DynamicFormGroup group)
                {
                    var groupData = new
                    {
                        ObjectType = "Group",
                        group.GroupId,
                        group.GroupName,
                        group.ParentGroupName,
                        group.Style,
                        group.Type,
                        group.GroupMetadata,
                        AttributeType = group.Attributes?.GetType().AssemblyQualifiedName,
                        AttributeData = group.Attributes != null ? SerializeAttribute(group.Attributes) : null,
                        Objects = SerializeObjects(group.Objects)
                    };
                    serializedObjects.Add(groupData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing object of type {obj.GetType().Name}: {ex.Message}");
            }
        }

        return serializedObjects;
    }

    public static DynamicForm Deserialize(string json, object parentObject)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                PropertyNameCaseInsensitive = true
            };

            var formData = JsonSerializer.Deserialize<JsonElement>(json);
            var form = new DynamicForm(parentObject)
            {
                FormId = formData.GetProperty("FormId").GetString() ?? Guid.NewGuid().ToString(),
                FormName = formData.GetProperty("FormName").GetString() ?? "",
                Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    formData.GetProperty("Metadata").GetRawText(), options) ?? new()
            };

            // Get the parent group data
            var parentGroupData = formData.GetProperty("ParentGroup");
            DeserializeGroup(parentGroupData, form.ParentGroup, parentObject);

            Console.WriteLine("DynamicForm deserialized successfully.");
            return form;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during deserialization: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    private static void DeserializeGroup(JsonElement groupData, DynamicFormGroup group, object parentObject)
    {
        group.Objects.Clear();

        var objects = groupData.GetProperty("Objects").EnumerateArray();
        foreach (var obj in objects)
        {
            try
            {
                var type = obj.GetProperty("ObjectType").GetString();
                if (type == "Field")
                {
                    var field = DeserializeField(obj, parentObject);
                    if (field != null)
                    {
                        group.Objects.Add(field);
                    }
                }
                else if (type == "Group")
                {
                    var childGroup = CreateGroupFromData(obj, parentObject);
                    if (childGroup != null)
                    {
                        DeserializeGroup(obj, childGroup, parentObject);
                        group.Objects.Add(childGroup);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing group object: {ex.Message}");
            }
        }
    }

    private static DynamicFormField? DeserializeField(JsonElement fieldData, object parentObject)
    {
        try
        {
            var propertyName = fieldData.GetProperty("PropertyName").GetString();
            var property = parentObject.GetType().GetProperty(propertyName ?? "");
            if (property == null) return null;

            var attributeType = Type.GetType(fieldData.GetProperty("AttributeType").GetString() ?? "");
            if (attributeType == null) return null;

            var attributeData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                fieldData.GetProperty("AttributeData").GetRawText());
            if (attributeData == null) return null;

            var attribute = CreateAttributeFromData(attributeType, attributeData);
            if (attribute is not DynamicFormFieldAttribute fieldAttribute) return null;

            var value = DeserializeValue(fieldData.GetProperty("Value").GetString() ?? "", 
                property.PropertyType, 
                Enum.Parse<DynamicFormFieldType>(fieldData.GetProperty("FieldType").GetString() ?? ""));

            if (value != null)
            {
                property.SetValue(parentObject, value);
            }

            return new DynamicFormField(
                parentObject,
                value,
                property,
                fieldAttribute,
                fieldData.GetProperty("ParentGroupName").GetString() ?? "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing field: {ex.Message}");
            return null;
        }
    }

    private static DynamicFormGroup? CreateGroupFromData(JsonElement groupData, object parentObject)
    {
        try
        {
            var attributeType = Type.GetType(groupData.GetProperty("AttributeType").GetString() ?? "");
            if (attributeType == null) return null;

            var attributeData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                groupData.GetProperty("AttributeData").GetRawText());
            if (attributeData == null) return null;

            var attribute = CreateAttributeFromData(attributeType, attributeData);
            if (attribute is not DynamicFormGroupAttribute groupAttribute) return null;

            return new DynamicFormGroup(
                parentObject,
                groupData.GetProperty("GroupName").GetString() ?? "",
                groupData.GetProperty("ParentGroupName").GetString() ?? "",
                Enum.Parse<DynamicFormGroupStyle>(groupData.GetProperty("Style").GetString() ?? "Basic"),
                Enum.Parse<DynamicFormLayout>(groupData.GetProperty("Type").GetString() ?? "Vertical"),
                groupAttribute);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating group from data: {ex.Message}");
            return null;
        }
    }

    private static Dictionary<string, object> SerializeAttribute(Attribute attribute)
    {
        var data = new Dictionary<string, object>();
        var properties = attribute.GetType().GetProperties();
        
        foreach (var prop in properties)
        {
            var value = prop.GetValue(attribute);
            if (value != null)
            {
                data[prop.Name] = value;
            }
        }

        return data;
    }

    private static Attribute? CreateAttributeFromData(Type attributeType, Dictionary<string, object> data)
    {
        var attribute = Activator.CreateInstance(attributeType) as Attribute;
        if (attribute == null) return null;

        var properties = attributeType.GetProperties().Where(p => p.CanWrite).ToList();
        foreach (var prop in properties)
        {
            if (data.TryGetValue(prop.Name, out var value))
            {
                // Convert JsonElement to the appropriate type
                if (value is JsonElement element)
                {
                    value = ConvertJsonElement(element, prop.PropertyType);
                }
                
                try
                {
                    prop.SetValue(attribute, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting property '{prop.Name}': {ex.Message}");
                    // Skip if we can't set the property
                }
            }
        }

        return attribute;
    }

    private static object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        if (targetType == typeof(string))
            return element.GetString();
        else if (targetType == typeof(int) || targetType == typeof(int?))
            return element.GetInt32();
        else if (targetType == typeof(bool) || targetType == typeof(bool?))
            return element.GetBoolean();
        else if (targetType == typeof(double) || targetType == typeof(double?))
            return element.GetDouble();
        else if (targetType.IsEnum)
            return Enum.Parse(targetType, element.GetString() ?? "");
        else
            return element.GetRawText();
    }

    private static string SerializeValue(object? value, DynamicFormFieldType fieldType)
    {
        if (value == null) return "";

        if (fieldType == DynamicFormFieldType.ColorPicker && value is byte[] colorBytes)
        {
            return StringColorConverter.Convert(colorBytes);
        }

        return JsonSerializer.Serialize(value);
    }

    private static object? DeserializeValue(string serializedValue, Type targetType, DynamicFormFieldType fieldType)
    {
        if (string.IsNullOrEmpty(serializedValue)) return null;

        try
        {
            if (fieldType == DynamicFormFieldType.ColorPicker)
            {
                return StringColorConverter.Convert(serializedValue);
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, serializedValue.Trim('"'));
            }

            var underlyingType = targetType.GetUnderlyingType();
            
            if (underlyingType == typeof(string))
            {
                return serializedValue.Trim('"');
            }
            else if (underlyingType == typeof(int))
            {
                return int.Parse(serializedValue);
            }
            else if (underlyingType == typeof(bool))
            {
                return bool.Parse(serializedValue.ToLower());
            }
            else if (underlyingType == typeof(double))
            {
                return double.Parse(serializedValue);
            }
            else if (underlyingType == typeof(DateTime))
            {
                return DateTime.Parse(serializedValue.Trim('"'));
            }
            else
            {
                return JsonSerializer.Deserialize(serializedValue, targetType);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing value: {ex.Message}");
            return null;
        }
    }

    public static void SaveToFile(DynamicForm form, string filePath)
    {
        Console.WriteLine($"Saving DynamicForm to file: {filePath}");
        try
        {
            File.WriteAllText(filePath, Serialize(form));
            Console.WriteLine("DynamicForm saved successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving DynamicForm to file: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    public static DynamicForm LoadFromFile(string filePath, object parentObject)
    {
        Console.WriteLine($"Loading DynamicForm from file: {filePath}");
        try
        {
            return Deserialize(File.ReadAllText(filePath), parentObject);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading DynamicForm from file: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    private static bool IsApplicablePlatform(DynamicFormPlatform attributePlatform, DynamicFormPlatform targetPlatform)
    {
        if (attributePlatform == DynamicFormPlatform.All || targetPlatform == DynamicFormPlatform.All)
            return true;
        
        return (attributePlatform & targetPlatform) != 0;
    }

    private static JsonSerializerOptions GetJsonOptions() => new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

   

    private static DynamicFormFieldAttribute CreateFieldAttribute(string type, string label, JsonElement config)
    {
        return type.ToLower() switch
        {
            "textbox" => new DynamicFormFieldTextBoxAttribute(label),
            "checkbox" => new DynamicFormFieldCheckBoxAttribute(
                config.GetProperty("Text").GetString() ?? label),
            "combobox" => new DynamicFormFieldComboBoxAttribute(
                config.GetProperty("OptionsProperty").GetString(),
                label),
            "slider" => new DynamicFormFieldSliderAttribute(
                config.GetProperty("Min").GetDouble(),
                config.GetProperty("Max").GetDouble(),
                label: label),
            "colorpicker" => new DynamicFormFieldColorPickerAttribute(label),
            "filepicker" => new DynamicFormFieldFilePickerAttribute(
                FilePickerType.OpenFile, 
                label: label),
            "numericupdown" => new DynamicFormFieldNumericUpDownAttribute(label: label),
            _ => new DynamicFormFieldTextBoxAttribute(label)
        };
    }
}

public class SerializedDynamicForm
{
    public string FormId { get; set; } = "";
    public string FormName { get; set; } = "";
    public Dictionary<string, object> Metadata { get; set; } = new();
    public SerializedDynamicFormGroup ParentGroup { get; set; } = new();
}

public class SerializedDynamicFormGroup : SerializedDynamicFormObject
{
    public string GroupId { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string ParentGroupName { get; set; } = "";
    public DynamicFormGroupStyle Style { get; set; }
    public DynamicFormLayout Type { get; set; }
    public Dictionary<string, object> GroupMetadata { get; set; } = new();
    public List<Dictionary<string, object>>? Attributes { get; set; }
    public List<SerializedDynamicFormObject> Objects { get; set; } = new();
}

public class SerializedDynamicFormField : SerializedDynamicFormObject
{
    public string FieldId { get; set; } = "";
    public string FieldName { get; set; } = "";
    public string FieldType { get; set; } = "";
    public bool IsRequired { get; set; }
    public string ValidationRules { get; set; } = "";
    public Dictionary<string, object> FieldMetadata { get; set; } = new();
    public string ParentGroupName { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public string? SerializedValue { get; set; }
    public string? ValueType { get; set; }
    public Dictionary<string, object>? AttributeData { get; set; }
}

public abstract class SerializedDynamicFormObject
{
    public string ObjectType { get; set; } = "";
} 