using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class CecilTestExtensions
{
    static CecilTestExtensions()
    {
        var path = new Uri(typeof(string).Assembly.Location).LocalPath;
        var parameters = new ReaderParameters(ReadingMode.Immediate)
        {
            ReadWrite = false
        };
        MsCoreLib = ModuleDefinition.ReadModule(path, parameters);
    }

    public static ModuleDefinition MsCoreLib;

    public static TypeDefinition GetTypeDefinition<T>(this ModuleDefinition moduleDefinition)
    {
        var replace = typeof(T).FullName.Replace("+", "/")
            //trim trailing generic info
            .Split('[')
            .First();
        return moduleDefinition.GetAllTypes()
            .First(x => x.FullName == replace);
    }

    public static string DisplayName(this TypeReference typeReference)
    {
        if (typeReference is GenericInstanceType genericInstanceType && genericInstanceType.HasGenericArguments)
        {
            return $"{typeReference.Name.Split('`').First()}<{string.Join(", ", genericInstanceType.GenericArguments.Select(c => c.DisplayName()))}>";
        }
        return typeReference.Name;
    }
}