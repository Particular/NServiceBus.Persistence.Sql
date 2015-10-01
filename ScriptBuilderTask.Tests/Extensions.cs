using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class Extensions
{

    public static TypeDefinition GetTypeDefinition<T>(this ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.GetAllTypes().First(x => x.FullName == typeof(T).FullName.Replace("+","/"));
    }
}