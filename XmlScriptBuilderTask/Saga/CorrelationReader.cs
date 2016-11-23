using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql.Xml;

class CorrelationReader
{

    public static CorrelationResult GetCorrelationMember(TypeDefinition sagaDataType)
    {
        CorrelationMember correlationMember = null;
        CorrelationMember transitionalMember = null;
        var withCorrelationAttribute = sagaDataType.MembersWithAttribute("NServiceBus.Persistence.Sql.Xml.CorrelationIdAttribute").ToList();
        var withTransitionalAttribute = sagaDataType.MembersWithAttribute("NServiceBus.Persistence.Sql.Xml.TransitionalCorrelationIdAttribute").ToList();

        if (withCorrelationAttribute.Count > 1)
        {
            throw new ErrorsException($"The type '{sagaDataType.FullName}' has multiple members marked with [CorrelationId]. Members: {string.Join(",", withCorrelationAttribute.Select(_ => _.Name))}");
        }
        if (withTransitionalAttribute.Count > 1)
        {
            throw new ErrorsException($"The type '{sagaDataType.FullName}' has multiple members marked with [TransitionalCorrelationId]. Members: {string.Join(",", withTransitionalAttribute.Select(_ => _.Name))}");
        }
        if (withCorrelationAttribute.Any())
        {
            var member = withCorrelationAttribute.Single();
            correlationMember = BuildConstraintMember(member);
        }
        if (withTransitionalAttribute.Any())
        {
            var member = withTransitionalAttribute.Single();
            transitionalMember = BuildConstraintMember(member);
        }

        if (correlationMember != null &&
            transitionalMember != null &&
            correlationMember.Name == transitionalMember.Name)
        {
            throw new ErrorsException($"The type '{sagaDataType.FullName}' has a [CorrelationId] applied to a member that also has a [TransitionalCorrelationId] applied. Member: {transitionalMember.Name}");
        }
        return new CorrelationResult
        {
            CorrelationMember = correlationMember,
            TransitionalCorrelationMember = transitionalMember
        };
    }

    static CorrelationMember BuildConstraintMember(IMemberDefinition member)
    {
        return new CorrelationMember
        {
            Name = member.Name,
            Type = GetConstraintMemberType(member)
        };
    }

    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    static CorrelationMemberType GetConstraintMemberType(IMemberDefinition member)
    {
        var memberType = member.MemberType();
        var fullName = memberType.FullName;
        if (
            fullName == typeof(Int16).FullName ||
            fullName == typeof(Int32).FullName ||
            fullName == typeof(Int64).FullName ||
            fullName == typeof(UInt16).FullName ||
            fullName == typeof(UInt32).FullName
            )
        {
            return CorrelationMemberType.Int;
        }
        if (fullName == typeof(Guid).FullName)
        {
            return CorrelationMemberType.Guid;
        }
        if (fullName == typeof(DateTime).FullName)
        {
            return CorrelationMemberType.DateTime;
        }
        if (fullName == typeof(DateTimeOffset).FullName)
        {
            return CorrelationMemberType.DateTimeOffset;
        }
        if (fullName == typeof(String).FullName)
        {
            return CorrelationMemberType.String;
        }
        throw new ErrorsException($"Could not convert '{fullName}' to {typeof(CorrelationMemberType).Name}.");
    }
}
