using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NServiceBus.Persistence.Sql;

static class CecilExtensions
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

    public static PropertyDefinition GetProperty(this TypeDefinition type, string propertyName)
    {
        var property = type.Properties.SingleOrDefault(_ => _.Name == propertyName);
        if (property != null)
        {
            return property;
        }
        throw new ErrorsException($@"Expected to find a property named '{propertyName}' on '{type.FullName}'.");
    }

    public static bool TryGetProperty(this TypeDefinition type, string propertyName, out PropertyDefinition property)
    {
        property = type.Properties.SingleOrDefault(_ => _.Name == propertyName);
        return property != null;
    }

    public static bool TryGetPropertyAssignment(this PropertyDefinition property, out string value)
    {
        value = null;
        var instructions = property.GetMethod.Body.Instructions;
        if (instructions.Count != 2)
        {
            return false;
        }
        if (instructions[1].OpCode != OpCodes.Ret)
        {
            return false;
        }
        var first = instructions[0];
        if (first.OpCode == OpCodes.Ldstr)
        {
            value = (string)first.Operand;
            return true;
        }
        if (first.OpCode == OpCodes.Ldnull)
        {
            return true;
        }
        return false;
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
            var debugInformation = method.DebugInformation;
            if (debugInformation == null)
            {
                continue;
            }
            foreach (var point in debugInformation.SequencePoints)
            {
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