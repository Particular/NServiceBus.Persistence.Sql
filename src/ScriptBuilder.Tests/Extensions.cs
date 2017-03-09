using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class Extensions
{

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
        var genericInstanceType = typeReference as GenericInstanceType;
        if (genericInstanceType != null && genericInstanceType.HasGenericArguments)
        {
            return $"{typeReference.Name.Split('`').First()}<{string.Join(", ", genericInstanceType.GenericArguments.Select(c => c.DisplayName()))}>";
        }
        return typeReference.Name;
    }
}