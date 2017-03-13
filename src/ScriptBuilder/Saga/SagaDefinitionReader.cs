using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SagaDefinitionReader
{

    public static bool TryGetSqlSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        ValidateIsNotDirectSaga(type);

        var attribute = type.GetSingleAttribute("NServiceBus.Persistence.Sql.SqlSagaAttribute");
        if (attribute == null)
        {
            ValidateDoesNotInheritFromSqlSaga(type);
            definition = null;
            return false;
        }
        CheckIsValidSaga(type);

        var correlation = attribute.GetArgument(0);
        if (string.IsNullOrWhiteSpace(correlation))
        {
            throw new ErrorsException($"The type '{type.FullName}' has a [CorrelatedSagaAttribute] with an empty or null correlationProperty parameter.");
        }
        var transitional = attribute.GetStringProperty("TransitionalCorrelationProperty");
        var tableSuffix = attribute.GetStringProperty("TableSuffix");

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

    static void CheckIsValidSaga(TypeDefinition type)
    {
        if (type.HasGenericParameters)
        {
            throw new ErrorsException($"The type '{type.FullName}' has a [SqlSagaAttribute] but has generic parameters.");
        }
        if (type.IsAbstract)
        {
            throw new ErrorsException($"The type '{type.FullName}' has a [SqlSagaAttribute] but has is abstract.");
        }
    }

    static void ValidateDoesNotInheritFromSqlSaga(TypeDefinition type)
    {
        if (type.IsAbstract || type.BaseType == null)
        {
            return;
        }
        var baseTypeFullName = type.BaseType.FullName;
        if (baseTypeFullName.StartsWith("NServiceBus.Persistence.Sql.SqlSaga"))
        {
            throw new ErrorsException($"The type '{type.FullName}' inherits from NServiceBus.Persistence.Sql.SqlSaga but does not contain a [CorrelatedSagaAttribute] or [AlwaysStartNewSagaAttribute].");
        }
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
        foreach (var method in sagaType.Methods)
        {
            var parameters = method.Parameters;
            if (method.Name != "ConfigureMapping" || parameters.Count != 1)
            {
                continue;
            }
            var parameterType = parameters[0].ParameterType;
            if (parameterType.FullName.StartsWith("NServiceBus.Persistence.Sql.MessagePropertyMapper"))
            {
                var genericInstanceType = (GenericInstanceType) parameterType;
                var argument = genericInstanceType.GenericArguments.Single();
                return ToTypeDefinition(sagaType, argument);
            }
        }
        throw new ErrorsException($"The type '{sagaType.FullName}' needs to override SqlSaga.ConfigureHowToFindSaga(MessagePropertyMapper).");
    }

    static TypeDefinition ToTypeDefinition(TypeDefinition sagaType, TypeReference argument)
    {
        var sagaDataType = argument as TypeDefinition;
        if (sagaDataType != null)
        {
            return sagaDataType;
        }
        throw new ErrorsException($"The type '{sagaType.FullName}' uses a SagaData type '{argument.FullName}' that is not defined in the same assembly.");
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