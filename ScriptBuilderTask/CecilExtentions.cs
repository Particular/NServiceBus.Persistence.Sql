using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public static class CecilExtentions
{
    public static bool ReferencesNServiceBus(this TypeReference type)
    {
        return type.Module.AssemblyReferences.Any(x => x.Name.StartsWith("NServiceBus.Core"));
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

    public static TypeReference GetBase(this TypeReference type)
    {
        var definition = type as TypeDefinition;
        if (definition != null)
        {
            return definition.BaseType;
        }
        var genericInstanceType = type as GenericInstanceType;
        if (genericInstanceType != null)
        {
            var elementTypeResolve = genericInstanceType.ElementType.Resolve();
            var baseTypeReference = elementTypeResolve.BaseType;
            var instanceType = baseTypeReference as GenericInstanceType;
            if (instanceType == null)
            {
                return baseTypeReference;
            }
            var instance = new GenericInstanceType(instanceType.ElementType);
            foreach (var genericArgument in instanceType.GenericArguments)
            {
                var indexOf = elementTypeResolve.GenericParameters.IndexOf(s => s.Name == genericArgument.Name);
                var typeReference = genericInstanceType.GenericArguments[indexOf];
                instance.GenericArguments.Add(typeReference);
            }
            return instance;
        }
        return type.Resolve().BaseType;
    }

    //public static bool ReferencesNServiceBus(this GenericInstanceType type)
    //{
    //    return type.ElementType.Module.AssemblyReferences.Any(x => x.Name.StartsWith("NServiceBus.Core"));
    //}

    public  static bool IsSaga(this TypeReference type)
    {
        return type.FullName.StartsWith("NServiceBus.Saga.Saga`1");
    }

    public static bool ContainsAttribute(this ICustomAttributeProvider property, string attributeName)
    {
        return property.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == attributeName);
    }

    public static int IndexOf<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {

        var retVal = 0;
        foreach (var item in items)
        {
            if (predicate(item)) return retVal;
            retVal++;
        }
        return -1;
    }
}