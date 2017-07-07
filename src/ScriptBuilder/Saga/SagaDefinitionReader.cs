using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SagaDefinitionReader
{

    public static bool TryGetSqlSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        ValidateIsNotDirectSaga(type);
        if (!IsSqlSaga(type))
        {
            definition = null;
            return false;
        }
        CheckIsValidSaga(type);

        var correlation = GetCorrelationPropertyName(type);
        var transitional = GetTransitionalCorrelationPropertyName(type);
        var tableSuffix = GetTableSuffix(type);

        SagaDefinitionValidator.ValidateSagaDefinition(correlation, type.FullName, transitional, tableSuffix);

        if (tableSuffix == null)
        {
            tableSuffix = type.Name;
        }

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

    static string GetTableSuffix(TypeDefinition type)
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

    static void ValidateIsNotDirectSaga(TypeDefinition type)
    {
        if (type.BaseType == null)
        {
            return;
        }
        var baseTypeFullName = type.BaseType.FullName;
        if (baseTypeFullName.StartsWith("NServiceBus.Saga"))
        {
            throw new ErrorsException($"The type '{type.FullName}' inherits from NServiceBus.Saga which is not supported. Inherit from NServiceBus.Persistence.Sql.SqlSaga.");
        }
    }

    static TypeDefinition GetSagaDataTypeFromSagaType(TypeDefinition sagaType)
    {
        var baseType = (GenericInstanceType) sagaType.BaseType;
        var sagaDataReference = baseType.GenericArguments.Single();
        var sagaDataType = sagaDataReference as TypeDefinition;
        if (sagaDataType != null)
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
        var propertyDefinition = sagaDataTypeDefinition.Properties.SingleOrDefault(x => x.Name == propertyName);

        if (propertyDefinition == null)
        {
            throw new ErrorsException($"Expected type '{sagaDataTypeDefinition.FullName}' to contain a property named '{propertyName}'.");
        }
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