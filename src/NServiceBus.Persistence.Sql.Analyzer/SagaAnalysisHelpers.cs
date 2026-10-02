namespace NServiceBus.Persistence.Sql.Analyzer;

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

static class SagaAnalysisHelpers
{
    public static INamedTypeSymbol? GetSagaDataType(INamedTypeSymbol sagaType)
    {
        var current = sagaType;
        while (current.BaseType is not null)
        {
            current = current.BaseType;
            var def = current.OriginalDefinition;
            if (def.Name == "Saga" && def is { IsGenericType: true, TypeParameters.Length: 1 } && def.ContainingNamespace.Name == "NServiceBus" && def.ContainingNamespace.ContainingNamespace.IsGlobalNamespace)
            {
                if (current.TypeArguments[0] is INamedTypeSymbol dataType)
                {
                    return dataType;
                }
            }
        }

        return null;
    }

    public static IPropertySymbol? GetCorrelationProperty(INamedTypeSymbol sagaDataType, string? propertyName)
    {
        if (propertyName is null)
        {
            return null;
        }

        for (var type = sagaDataType; type is not null && type.ContainingAssembly.Name != "NServiceBus.Core"; type = type.BaseType)
        {
            var propSymbol = type.GetMembers(propertyName)
                .OfType<IPropertySymbol>()
                .FirstOrDefault(symbol => symbol.Name == propertyName);

            if (propSymbol is not null)
            {
                return propSymbol;
            }
        }

        return null;
    }

    public static bool TryGetCorrelationSqlPropertyType(ITypeSymbol type, [NotNullWhen(true)] out string? sqlPropertyType)
    {
        sqlPropertyType = null;

        // Cases must cover allowed types in NServiceBus.SagaMapper:AllowedCorrelationPropertyTypes
        // Output value must match NServiceBus.Persistence.Sql.ScriptBuilder.CorrelationPropertyType
        if (type.SpecialType == SpecialType.System_String)
        {
            sqlPropertyType = "String";
            return true;
        }

        if (type.SpecialType is SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int16 or SpecialType.System_UInt16)
        {
            sqlPropertyType = "Int";
            return true;
        }

        if (type is { Name: "Guid", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } })
        {
            sqlPropertyType = "Guid";
            return true;
        }

        // Not going to cover DateTime or DateTimeOffset
        // Don't map invalid values, just return null - Core analyzers already report on this
        return false;
    }
}
