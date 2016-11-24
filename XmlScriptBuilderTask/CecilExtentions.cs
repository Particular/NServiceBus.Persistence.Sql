using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public static class CecilExtentions
{

    public static CustomAttribute GetSingleAttribute(this TypeDefinition type, string attributeName)
    {
        return type.CustomAttributes.SingleOrDefault(x => x.AttributeType.FullName == attributeName);
    }

    public static TypeReference MemberType(this IMemberDefinition member)
    {
        var propertyDefinition = member as PropertyDefinition;
        if (propertyDefinition != null)
        {
            return propertyDefinition.PropertyType;
        }
        var fieldDefinition = member as FieldDefinition;
        if (fieldDefinition != null)
        {
            return fieldDefinition.FieldType;
        }
        throw new Exception($"Expected '{member.FullName}' to be either a property or a field.");
    }

    public static IEnumerable<IMemberDefinition> Members(this TypeDefinition type)
    {
        foreach (var member in type.Properties)
        {
            yield return member;
        }
        foreach (var member in type.Fields)
        {
            yield return member;
        }
    }
    public static IEnumerable<TypeDefinition> AllClasses(this ModuleDefinition module)
    {
        return module.GetTypes().Where(x => x.IsClass);
    }

    public static string GetFileName(this TypeDefinition type)
    {
        foreach (var method in type.Methods)
        {
            var body = method.Body;
            if (body?.Instructions == null)
            {
                continue;
            }
            foreach (var instruction in body.Instructions)
            {
                var point = instruction.SequencePoint;
                if (point?.Document?.Url == null)
                {
                    continue;
                }
                return point.Document.Url;
            }
        }
        return null;
    }

}