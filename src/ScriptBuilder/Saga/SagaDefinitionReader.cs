using System;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SagaDefinitionReader
{
    public static bool TryGetSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        return TryGetSqlSagaDefinition(type, out definition)
               || TryGetCoreSagaDefinition(type, out definition);
    }

    static bool TryGetSqlSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        if (!IsSqlSaga(type))
        {
            definition = null;
            return false;
        }
        CheckIsValidSaga(type);

        if (type.GetSingleAttribute("NServiceBus.Persistence.Sql.SqlSagaAttribute") != null)
        {
            throw new Exception("[SqlSaga] attribute is invalid on a class inheriting SqlSaga<T>. To provide CorrelationId, TransitionalCorrelationId, or TableSuffix override the corresponding properties on the SqlSaga<T> base class instead.");
        }

        var correlation = GetCorrelationPropertyName(type);
        var transitional = GetTransitionalCorrelationPropertyName(type);
        var tableSuffix = GetSqlSagaTableSuffix(type);

        SagaDefinitionValidator.ValidateSagaDefinition(correlation, type.FullName, transitional, tableSuffix);

        tableSuffix ??= type.Name;

        var sagaDataType = GetSagaDataTypeFromSagaType(type);

        definition = new SagaDefinition
        (
            correlationProperty: BuildConstraintProperty(sagaDataType, correlation),
            transitionalCorrelationProperty: BuildConstraintProperty(sagaDataType, transitional),
            tableSuffix: tableSuffix,
            name: type.FullName
        );
        return true;
    }

    static bool TryGetCoreSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        definition = null;

        // Class must directly inherit from NServiceBus.Saga<T> so that no tricks can be pulled from an intermediate class
        if (type.BaseType is not GenericInstanceType baseType || !baseType.FullName.StartsWith("NServiceBus.Saga") || baseType.GenericArguments.Count != 1)
        {
            return false;
        }

        CheckIsValidSaga(type);

        string correlationId = null;
        string transitionalId = null;
        string tableSuffix = null;

        var attribute = type.GetSingleAttribute("NServiceBus.Persistence.Sql.SqlSagaAttribute");
        if (attribute != null)
        {
            var args = attribute.ConstructorArguments;
            correlationId = (string)args[0].Value;
            transitionalId = (string)args[1].Value;
            tableSuffix = (string)args[2].Value;
        }

        var sagaDataType = GetSagaDataTypeFromSagaType(type);

        correlationId ??= GetCoreSagaCorrelationId(type, sagaDataType);

        SagaDefinitionValidator.ValidateSagaDefinition(correlationId, type.FullName, transitionalId, tableSuffix);

        tableSuffix ??= type.Name;

        definition = new SagaDefinition
        (
            correlationProperty: BuildConstraintProperty(sagaDataType, correlationId),
            transitionalCorrelationProperty: BuildConstraintProperty(sagaDataType, transitionalId),
            tableSuffix: tableSuffix,
            name: type.FullName
        );
        return true;
    }

    static string GetCoreSagaCorrelationId(TypeDefinition type, TypeDefinition sagaDataType)
    {
        var instructions = InstructionAnalyzer.GetConfigureHowToFindSagaInstructions(type);

        if (InstructionAnalyzer.ContainsBranchingLogic(instructions))
        {
            throw new ErrorsException("Looping & branching statements are not allowed in a ConfigureHowToFindSaga method.");
        }

        if (InstructionAnalyzer.CallsUnmanagedMethods(instructions))
        {
            throw new ErrorsException("Calling unmanaged code is not allowed in a ConfigureHowToFindSaga method.");
        }

        if (InstructionAnalyzer.CallsUnexpectedMethods(instructions))
        {
            throw new ErrorsException("Unable to determine Saga correlation property because an unexpected method call was detected in the ConfigureHowToFindSaga method.");
        }

        var correlationId = sagaDataType.FindInTypeHierarchy(t => InstructionAnalyzer.GetCorrelationId(instructions, t.FullName));

        return correlationId;
    }

    static string GetCorrelationPropertyName(TypeDefinition type)
    {
        var property = type.GetProperty("CorrelationPropertyName");
        if (property.TryGetPropertyAssignment(out var value))
        {
            return value;
        }
        throw new ErrorsException(
            $@"Only a direct string (or null) return is allowed in '{type.FullName}.CorrelationPropertyName'.
For example: protected override string CorrelationPropertyName => nameof(SagaData.TheProperty);
When all messages are mapped using finders then use the following: protected override string CorrelationPropertyName => null;");
    }


    static string GetTransitionalCorrelationPropertyName(TypeDefinition type)
    {
        if (!type.TryGetProperty("TransitionalCorrelationPropertyName", out var property))
        {
            return null;
        }
        if (property.TryGetPropertyAssignment(out var value))
        {
            return value;
        }
        throw new ErrorsException(
            $@"Only a direct string return is allowed in '{type.FullName}.TransitionalCorrelationPropertyName'.
For example: protected override string TransitionalCorrelationPropertyName => nameof(SagaData.TheProperty);");
    }

    static string GetSqlSagaTableSuffix(TypeDefinition type)
    {
        if (!type.TryGetProperty("TableSuffix", out var property))
        {
            return null;
        }
        if (property.TryGetPropertyAssignment(out var value))
        {
            return value;
        }
        throw new ErrorsException(
            $@"Only a direct string return is allowed in '{type.FullName}.TableSuffix'.
For example: protected override string TableSuffix => ""TheCustomTableSuffix"";");
    }

    static void CheckIsValidSaga(TypeDefinition type)
    {
        if (type.HasGenericParameters)
        {
            throw new ErrorsException($"The type '{type.FullName}' has generic parameters.");
        }
        if (type.IsAbstract)
        {
            throw new ErrorsException($"The type '{type.FullName}' is abstract.");
        }
    }

    static bool IsSqlSaga(TypeDefinition type)
    {
        var baseType = type.BaseType;
        if (baseType == null)
        {
            return false;
        }
        return baseType.Scope.Name.StartsWith("NServiceBus.Persistence.Sql") &&
               baseType.FullName.StartsWith("NServiceBus.Persistence.Sql.SqlSaga");
    }

    static TypeDefinition GetSagaDataTypeFromSagaType(TypeDefinition sagaType)
    {
        var baseType = (GenericInstanceType)sagaType.BaseType;
        var sagaDataReference = baseType.GenericArguments.Single();
        if (sagaDataReference is TypeDefinition sagaDataType)
        {
            return sagaDataType;
        }
        throw new ErrorsException($"The type '{sagaType.FullName}' uses a SagaData type '{sagaDataReference.FullName}' that is not defined in the same assembly.");
    }

    static CorrelationProperty BuildConstraintProperty(TypeDefinition sagaDataTypeDefinition, string propertyName)
    {
        if (propertyName == null)
        {
            return null;
        }

        var propertyDefinition = sagaDataTypeDefinition.FindInTypeHierarchy(t => t.Properties.SingleOrDefault(x => x.Name == propertyName))
                                 ?? throw new ErrorsException($"Expected type '{sagaDataTypeDefinition.FullName}' to contain a property named '{propertyName}'.");
        if (propertyDefinition.SetMethod == null)
        {
            throw new ErrorsException($"The type '{sagaDataTypeDefinition.FullName}' has a constraint property '{propertyName}' that is read-only.");
        }
        return BuildConstraintProperty(propertyDefinition);
    }

    static CorrelationProperty BuildConstraintProperty(PropertyDefinition member)
    {
        var propertyType = member.PropertyType;
        return new CorrelationProperty
        (
            name: member.Name,
            type: CorrelationPropertyTypeReader.GetCorrelationPropertyType(propertyType)
        );
    }
}