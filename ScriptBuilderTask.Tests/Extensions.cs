using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class Extensions
{

    public static TypeDefinition GetTypeDefinition<T>(this ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.GetAllTypes().First(x => x.FullName == typeof(T).FullName.Replace("+","/"));
    }

    public static string DisplayName(this TypeReference typeReference)
    {
        var genericInstanceType = typeReference as GenericInstanceType;
        if (genericInstanceType != null && genericInstanceType.HasGenericArguments)
        {
            return typeReference.Name.Split('`').First() + "<" + string.Join(", ", genericInstanceType.GenericArguments.Select(c => c.DisplayName())) + ">";
        }
        return typeReference.Name;
    }
}