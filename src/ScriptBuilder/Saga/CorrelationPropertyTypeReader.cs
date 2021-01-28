using System;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class CorrelationPropertyTypeReader
{
    internal static CorrelationPropertyType GetCorrelationPropertyType(TypeReference typeReference)
    {
        return GetCorrelationPropertyType(typeReference.FullName);
    }
    internal static CorrelationPropertyType GetCorrelationPropertyType(Type type)
    {
        return GetCorrelationPropertyType(type.FullName);
    }

    static CorrelationPropertyType GetCorrelationPropertyType(string fullName)
    {
        if (
            fullName == typeof(short).FullName ||
            fullName == typeof(int).FullName ||
            fullName == typeof(long).FullName ||
            fullName == typeof(ushort).FullName ||
            fullName == typeof(uint).FullName
        )
        {
            return CorrelationPropertyType.Int;
        }
        if (fullName == typeof(Guid).FullName)
        {
            return CorrelationPropertyType.Guid;
        }
        if (fullName == typeof(DateTime).FullName)
        {
            return CorrelationPropertyType.DateTime;
        }
        if (fullName == typeof(DateTimeOffset).FullName)
        {
            return CorrelationPropertyType.DateTimeOffset;
        }
        if (fullName == typeof(string).FullName)
        {
            return CorrelationPropertyType.String;
        }
        throw new ErrorsException($"Could not convert '{fullName}' to {nameof(CorrelationPropertyType)}.");
    }
}