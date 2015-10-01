using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public static class CecilExtentions
{
    public static IEnumerable<TypeDefinition> GetRootTypes(this ModuleDefinition module)
    {
        return module.GetTypes().Where(type => !(type.BaseType is TypeDefinition));
    }
}