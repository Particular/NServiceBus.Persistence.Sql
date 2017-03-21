using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Serialization;
using NServiceBus;

//TODO:
static class SagaPropertyFilter
{
    public static List<JsonProperty> FilterProperties(IList<JsonProperty> properties)
    {
        return properties.Where(property => !IsSagaProperty(property))
            .ToList();
    }

    static bool IsSagaProperty(JsonProperty jsonProperty)
    {
        var declaringType = jsonProperty.DeclaringType;
        if (!declaringType.IsAssignableFrom(typeof(IContainSagaData)))
        {
            return false;
        }

        return declaringType
            .GetInterfaceMap(typeof(IContainSagaData))
            .InterfaceMethods.Any(info => jsonProperty.UnderlyingName == info.Name);
    }
}