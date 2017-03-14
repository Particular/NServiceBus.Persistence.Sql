using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

static class CecilExtentions
{

    public static string GetStringProperty(this CustomAttribute attribute, string name)
    {
        return (string)attribute.Properties
            .SingleOrDefault(argument => argument.Name == name)
            .Argument.Value;
    }

    public static bool GetBoolProperty(this CustomAttribute attribute, string name)
    {
        var value = attribute.Properties
            .SingleOrDefault(argument => argument.Name == name)
            .Argument.Value;
        return value != null && (bool)value;
    }

    public static CustomAttribute GetSingleAttribute(this TypeDefinition type, string attributeName)
    {
        return type.CustomAttributes.SingleOrDefault(x => x.AttributeType.FullName == attributeName);
    }

    public static IEnumerable<TypeDefinition> AllClasses(this ModuleDefinition module)
    {
        return module.GetTypes()
            .Where(x => x.IsClass);
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