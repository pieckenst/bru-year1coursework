using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using DynamicForms.Library.Core.Attributes;
using DynamicForms.Library.Core.Shared;

namespace DynamicForms.Library.Core;

public class DynamicFormGroup : DynamicFormObject
{
    private static readonly HashSet<object> _processingObjects = new();

    [JsonConstructor]
    public DynamicFormGroup()
    {
        Objects = new List<DynamicFormObject>();
    }

    public DynamicFormGroup(object value, string parentGroupName)
    {
        Value = value;
        ParentGroupName = parentGroupName;
        GroupName = "";
        Style = DynamicFormGroupStyle.Basic;
        Type = DynamicFormLayout.Vertical;
        Objects = new List<DynamicFormObject>();
        GroupId = Guid.NewGuid().ToString();
        
        try
        {
            AddFormObjects(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in DynamicFormGroup constructor: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }
    
    public DynamicFormGroup(object value, string groupName, string parentGroupName, DynamicFormGroupStyle style, DynamicFormLayout type, DynamicFormGroupAttribute attributes)
    {
        Value = value;
        GroupName = groupName;
        ParentGroupName = parentGroupName;
        Style = style;
        Type = type;
        Attributes = attributes;
        Objects = new List<DynamicFormObject>();
        GroupId = Guid.NewGuid().ToString();
        
        try
        {
            AddFormObjects(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in DynamicFormGroup constructor with groupName '{groupName}': {ex.Message}");
            throw; // Re-throw the exception after logging
        }
    }

    // Core properties
    public string GroupName { get; init; }
    public DynamicFormGroupStyle Style { get; init; }
    public DynamicFormLayout Type { get; init; }
    public override bool IsGroup => true;
    public override string ParentGroupName { get; init; }

    [JsonIgnore]
    public DynamicFormGroupAttribute? Attributes { get; private set; }

    [JsonIgnore] 
    public object? Value { get; private set; }

    // Serializable properties
    public string GroupId { get; init; }
    public Dictionary<string, object> GroupMetadata { get; set; } = new();
    public List<DynamicFormObject> Objects { get; set; }

    private void AddFormObjects(object value)
    {
        // If we're already processing this object, skip to prevent recursion
        if (!_processingObjects.Add(value)) return;
        
        try
        {
            var subGroupAttributes = value.GetType().GetCustomAttributes()
                .Where(x => x is DynamicFormGroupAttribute attr && IsApplicablePlatform(attr.Platforms))
                .Cast<DynamicFormGroupAttribute>()
                .OrderBy(x => x.Order)
                .ToList();

            var subGroups = subGroupAttributes
                .Where(x => x.ParentGroup == null)
                .Select(x => new DynamicFormGroup(value, x.Name, GroupName, x.Style, x.Type, x))
                .ToList();

            var subGroupMap = subGroups.ToDictionary(x => x.GroupName, x => x);

            foreach (var childSubgroupAttribute in subGroupAttributes.Where(x => x.ParentGroup != null))
            {
                var parentSubgroup = subGroups.FirstOrDefault(x => x.GroupName == childSubgroupAttribute.ParentGroup) ??
                                   subGroups.First();
                var childSubgroup = new DynamicFormGroup(value, childSubgroupAttribute.Name, parentSubgroup.GroupName,
                    childSubgroupAttribute.Style, childSubgroupAttribute.Type, childSubgroupAttribute);
                parentSubgroup.Objects.Add(childSubgroup);
                subGroupMap.Add(childSubgroupAttribute.Name, childSubgroup);
            }

            var hasSubGroups = subGroups.Count != 0;
            Objects.AddRange(subGroups);

            foreach (var childObject in GetObjectsFrom(value))
            {
                if (hasSubGroups)
                {
                    subGroupMap.TryGetValue(childObject.ParentGroupName, out var parentGroup);
                    parentGroup ??= subGroups.First();
                    parentGroup.Objects.Add(childObject);
                }
                else
                {
                    Objects.Add(childObject);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding form objects: {ex.Message}");
            throw; // Re-throw the exception after logging
        }
        finally
        {
            _processingObjects.Remove(value);
        }
    }
    
    private List<DynamicFormObject> GetObjectsFrom(object parentObject)
    {
        List<DynamicFormObject> toReturn = new();

        try
        {
            var properties = parentObject.GetType().GetProperties()
                .Select(x => (IsProperty: true,
                    Property: x as object,
                    Attributes: x.GetCustomAttributes(true).FirstOrDefault(a => a is DynamicFormObjectAttribute attr && IsApplicablePlatform(attr.Platforms))))
                .Where(x => x.Attributes != null);
            
            var events = parentObject.GetType().GetEvents()
                .Select(x => (IsProperty: false,
                    Property: x as object,
                    Attributes: x.GetCustomAttributes(true).FirstOrDefault(a => a is DynamicFormObjectAttribute attr && IsApplicablePlatform(attr.Platforms))))
                .Where(x => x.Attributes != null);

            var items = properties.Concat(events).OrderBy(x => ((DynamicFormObjectAttribute)x.Attributes!).Order);
            
            foreach (var item in items)
            {
                if (item.IsProperty)
                {
                    var property = (PropertyInfo)item.Property;
                    var propValue = property.GetValue(parentObject);
                    if (item.Attributes is DynamicFormFieldAttribute fieldAttribute)
                    {
                        if (fieldAttribute.AllowedTypes != null)
                        {
                            var propertyType = property.PropertyType.GetUnderlyingType();
                            if (!fieldAttribute.AllowedTypes.Contains(propertyType))
                            {
                                var allowedNames = string.Join(", ", fieldAttribute.AllowedTypes.Select(x => x.Name));
                                throw new InvalidOperationException(
                                    $"Property {property.Name} type {property.PropertyType.Name} is not in the allowed type list of: {allowedNames}");
                            }
                        }

                        toReturn.Add(new DynamicFormField(parentObject, propValue, property, fieldAttribute, fieldAttribute.GroupName));
                    } 
                    else if (item.Attributes is DynamicFormObjectAttribute subObjectAttribute)
                    {
                        if (propValue == null)
                        {
                            propValue = Activator.CreateInstance(property.PropertyType.GetUnderlyingType()) ?? throw new InvalidOperationException("DynamicFormGroups cannot be null");
                        }
                        
                        toReturn.Add(new DynamicFormGroup(propValue, subObjectAttribute.GroupName));
                    }
                }
                else
                {
                    var eventInfo = (EventInfo)item.Property;
                    var attribute = (DynamicFormFieldButtonAttribute)item.Attributes!;
                    toReturn.Add(new DynamicFormField(parentObject, eventInfo.Name, null, attribute, attribute.GroupName));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting objects from parent object: {ex.Message}");
            throw; // Re-throw the exception after logging
        }

        return toReturn;
    }

    private bool IsApplicablePlatform(DynamicFormPlatform platforms)
    {
        if (platforms == DynamicFormPlatform.All)
        {
            return true;
        }
        
        if (OperatingSystem.IsWindows())
        {
            return (platforms & DynamicFormPlatform.Windows) == DynamicFormPlatform.Windows;
        }
        else if (OperatingSystem.IsLinux())
        {
            return (platforms & DynamicFormPlatform.Linux) == DynamicFormPlatform.Linux;
        }
        else if (OperatingSystem.IsMacOS())
        {
            return (platforms & DynamicFormPlatform.MacOS) == DynamicFormPlatform.MacOS;
        }

        return false;
    }

    public void ApplyTemplate(DynamicFormGroup template)
    {
        // Create a new group with the template values
        var newGroup = new DynamicFormGroup(
            Value ?? throw new InvalidOperationException("Value cannot be null"),
            template.GroupName,  // groupName
            template.ParentGroupName,  // parentGroupName
            template.Style,  // style
            template.Type,  // type
            template.Attributes ?? throw new InvalidOperationException("Template attributes cannot be null")  // attributes
        );

        // Copy over the metadata
        GroupMetadata = new Dictionary<string, object>(template.GroupMetadata);

        // Clear existing objects
        Objects.Clear();

        // Process each object in the template
        foreach (var obj in template.Objects)
        {
            try
            {
                if (obj is DynamicFormField field)
                {
                    var property = Value?.GetType().GetProperty(field.PropertyName);
                    if (property != null)
                    {
                        var newField = new DynamicFormField(Value, property.GetValue(Value), 
                            property, field.Attributes, field.ParentGroupName)
                        {
                            FieldId = field.FieldId,
                            FieldName = field.FieldName,
                            ValidationRules = field.ValidationRules,
                            FieldMetadata = new Dictionary<string, object>(field.FieldMetadata)
                        };
                        Objects.Add(newField);
                    }
                }
                else if (obj is DynamicFormGroup group)
                {
                    // Create new group with template values
                    var childGroup = new DynamicFormGroup(Value, group.GroupName, 
                        group.ParentGroupName, group.Style, group.Type, group.Attributes);
                    
                    // Apply template recursively
                    childGroup.ApplyTemplate(group);
                    Objects.Add(childGroup);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying template for object: {ex.Message}");
                throw; // Re-throw the exception after logging
            }
        }

        // Copy over any remaining properties that aren't init-only
        if (template.Attributes != null)
        {
            Attributes = template.Attributes;
        }
    }
}