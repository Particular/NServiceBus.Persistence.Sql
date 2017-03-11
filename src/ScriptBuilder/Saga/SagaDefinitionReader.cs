using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;
using NServiceBus.Persistence.Sql.ScriptBuilder;

static class SagaDefinitionReader
{

    public static bool TryGetSqlSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        ValidateIsNotDirectSaga(type);

        var correlationAttribute = type.GetSingleAttribute("NServiceBus.Persistence.Sql.CorrelatedSagaAttribute");
        var alwaysStartAttribute = type.GetSingleAttribute("NServiceBus.Persistence.Sql.AlwaysStartNewSagaAttribute");
        if (correlationAttribute == null && alwaysStartAttribute == null)
        {
            ValidateDoesNotInheritFromSqlSaga(type);
            definition = null;
            return false;
        }

        CheckIsValidSaga(type);

        if (correlationAttribute != null && alwaysStartAttribute != null)
        {
            throw new ErrorsException($"The type '{type.FullName}' contains both a [CorrelatedSagaAttribute] and [AlwaysStartNewSagaAttribute].");
        }
        if (correlationAttribute != null)
        {
            definition = GetCorrelationSagaDefinition(type, correlationAttribute);
            return true;
        }
        definition = GetAlwaysStartSagaDefinition(type, alwaysStartAttribute, type.FullName);
        return true;
    }

    static SagaDefinition GetAlwaysStartSagaDefinition(TypeDefinition type, CustomAttribute alwaysStartAttribute, string typeFullName)
    {
        var tableSuffix = alwaysStartAttribute.GetProperty("TableSuffix");
        SagaDefinitionValidator.ValidateTableSuffix(typeFullName, tableSuffix);

        if (tableSuffix == null)
        {
            tableSuffix = type.Name;
        }

        return new SagaDefinition
        (
            tableSuffix: tableSuffix,
            name: type.FullName
        );
    }

    static SagaDefinition GetCorrelationSagaDefinition(TypeDefinition type, CustomAttribute correlationAttribute)
    {
        var correlation = correlationAttribute.GetArgument(0);
        if (string.IsNullOrWhiteSpace(correlation))
        {
            throw new ErrorsException($"The type '{type.FullName}' has a [CorrelatedSagaAttribute] with an empty or null correlationProperty parameter.");
        }
        var transitional = correlationAttribute.GetProperty("TransitionalCorrelationProperty");
        var tableSuffix = correlationAttribute.GetProperty("TableSuffix");
        SagaDefinitionValidator.ValidateSagaDefinition(correlation, type.FullName, transitional);
        SagaDefinitionValidator.ValidateTableSuffix(type.FullName, tableSuffix);

        if (tableSuffix == null)
        {
            tableSuffix = type.Name;
        }

        var sagaDataType = GetSagaDataTypeFromSagaType(type);

        return new SagaDefinition
        (
            correlationProperty: BuildConstraintProperty(sagaDataType, correlation),
            transitionalCorrelationProperty: BuildConstraintProperty(sagaDataType, transitional),
            tableSuffix: tableSuffix,
            name: type.FullName
        );
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
        if (type.BaseType != null)
        {
            var baseTypeFullName = type.BaseType.FullName;
            if (baseTypeFullName.StartsWith("NServiceBus.Saga"))
            {
                throw new ErrorsException($"The type '{type.FullName}' inherits from NServiceBus.Saga which is not supported. Inherit from NServiceBus.Persistence.Sql.SqlSaga.");
            }
        }
    }

    static void CheckIsValidSaga(TypeDefinition type)
    {
        if (type.HasGenericParameters)
        {
            throw new ErrorsException($"The type '{type.FullName}' has a [CorrelatedSagaAttribute] or [AlwaysStartNewSagaAttribute] but has generic parameters.");
        }
        if (type.IsAbstract)
        {
            throw new ErrorsException($"The type '{type.FullName}' has a [CorrelatedSagaAttribute] or [AlwaysStartNewSagaAttribute] but has is abstract.");
        }
    }

    static string GetProperty(this CustomAttribute attribute, string name)
    {
        return (string)attribute.Properties
            .SingleOrDefault(argument => argument.Name == name)
            .Argument.Value;
    }

    static string GetArgument(this CustomAttribute attribute, int index)
    {
        return (string) attribute.ConstructorArguments[index].Value;
    }

    static TypeDefinition GetSagaDataTypeFromSagaType(TypeDefinition sagaType)
    {
        foreach (var method in sagaType.Methods)
        {
            var parameters = method.Parameters;
            if (method.Name == "ConfigureMapping")
            {
                if (parameters.Count == 1)
                {
                    var parameterType = parameters[0].ParameterType;
                    var parameterTypeName = parameterType.Name;
                    if (parameterTypeName.StartsWith("MessagePropertyMapper"))
                    {
                        var genericInstanceType = (GenericInstanceType)parameterType;
                        var argument = genericInstanceType.GenericArguments.Single();
                        return ToTypeDefinition(sagaType, argument);
                    }
                }
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
        throw new ErrorsException($"The type '{sagaType.FullName}' uses a SagaData type not defined in the same assembly.");
    }

    static CorrelationProperty BuildConstraintProperty(TypeDefinition sagaDataType, string propertyName)
    {
        if (propertyName == null)
        {
            return null;
        }
        var property = sagaDataType.Properties.SingleOrDefault(x => x.Name == propertyName);

        if (property != null)
        {
            return BuildConstraintProperty(property);
        }
        throw new ErrorsException($"Expected type '{sagaDataType.FullName}' to contain a property named '{propertyName}'.");

        //todo: verify not readonly
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