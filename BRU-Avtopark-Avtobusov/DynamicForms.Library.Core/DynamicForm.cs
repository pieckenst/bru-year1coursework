using System;
using System.Text.Json.Serialization;
using System.IO;
using DynamicForms.Library.Core.Attributes;
using DynamicForms.Library.Core.Serialization;

namespace DynamicForms.Library.Core;

public class DynamicForm
{
    public DynamicForm(object parentObject)
    {
        ParentGroup = new DynamicFormGroup(parentObject, "");
        FormId = Guid.NewGuid().ToString();
        Console.WriteLine($"DynamicForm created with FormId: {FormId}");
    }

    // Constructor for deserialization
    [JsonConstructor]
    public DynamicForm()
    {
        FormId = Guid.NewGuid().ToString();
        Console.WriteLine($"DynamicForm deserialized with FormId: {FormId}");
    }
    
    public DynamicFormGroup ParentGroup { get; init; }
    
    // Properties for serialization support
    public string FormId { get; set; }
    public string FormName { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Serialization methods that delegate to DynamicFormSerializer
    public string SaveToJson()
    {
        try
        {
            string json = DynamicFormSerializer.Serialize(this);
            Console.WriteLine("DynamicForm serialized to JSON successfully.");
            return json;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error serializing DynamicForm to JSON: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    public void SaveToFile(string filePath)
    {
        try
        {
            DynamicFormSerializer.SaveToFile(this, filePath);
            Console.WriteLine($"DynamicForm saved to file: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving DynamicForm to file: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    public static DynamicForm LoadFromJson(string json, object parentObject)
    {
        try
        {
            DynamicForm form = DynamicFormSerializer.Deserialize(json, parentObject);
            Console.WriteLine("DynamicForm loaded from JSON successfully.");
            return form;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading DynamicForm from JSON: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    public static DynamicForm LoadFromFile(string filePath, object parentObject)
    {
        try
        {
            DynamicForm form = DynamicFormSerializer.LoadFromFile(filePath, parentObject);
            Console.WriteLine($"DynamicForm loaded from file: {filePath}");
            return form;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading DynamicForm from file: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }
}