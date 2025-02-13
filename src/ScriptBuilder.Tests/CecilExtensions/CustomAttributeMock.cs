using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

public class CustomAttributeMock : ICustomAttribute
{
#pragma warning disable IDE0028
    public CustomAttributeMock(Dictionary<string, object> dictionary) => Properties = new Collection<CustomAttributeNamedArgument>(BuildProperties(dictionary).ToArray());
#pragma warning restore IDE0028

    IEnumerable<CustomAttributeNamedArgument> BuildProperties(Dictionary<string, object> objects)
    {
        return objects.Select(kv => new CustomAttributeNamedArgument(kv.Key, new CustomAttributeArgument(GetTypeReference(kv.Value), kv.Value)));
    }

    TypeReference GetTypeReference(object value)
    {
        if (value is string)
        {
            return CecilTestExtensions.MsCoreLib.TypeSystem.String;
        }
        if (value is bool)
        {
            return CecilTestExtensions.MsCoreLib.TypeSystem.Boolean;
        }
        throw new Exception();
    }

    public TypeReference AttributeType => throw new NotImplementedException();
    public bool HasFields => throw new NotImplementedException();
    public bool HasProperties => true;
    public Collection<CustomAttributeNamedArgument> Fields => throw new NotImplementedException();

    public Collection<CustomAttributeNamedArgument> Properties
    {
        get;
    }

    public bool HasConstructorArguments => throw new NotImplementedException();

    public Collection<CustomAttributeArgument> ConstructorArguments => throw new NotImplementedException();
}