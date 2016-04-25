using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

class CorrelationReader
{

    public static CorrelationResult GetCorrelationMember(TypeDefinition type)
    {
        var members = GetCorrelationProperties(type).ToList();

        if (members.Count == 0)
        {
            return new CorrelationResult();
        }
        if (members.Count > 1)
        {
            return new CorrelationResult
            {
                Error = $"The type '{type.FullName}' has multiple correlation members. Members: {string.Join(",", members)}",
                Errored = true,
            };
        }
        var member = members.Single();

        return new CorrelationResult
        {
            Name = member.Name
        };
    }

    public static IEnumerable<IMemberDefinition> GetCorrelationProperties(TypeDefinition type)
    {
        var attributeName = "NServiceBus.Persistence.SqlServerXml.CorrelationIdAttribute";
        foreach (var member in type.Properties)
        {
            if (member.ContainsAttribute(attributeName))
            {
                yield return member;
            }
        }
        foreach (var member in type.Fields)
        {
            if (member.ContainsAttribute(attributeName))
            {
                yield return member;
            }
        }
    }

}
