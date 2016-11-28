using System;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;

class SagaDefinitionReader
{

    public static bool TryGetSqlSagaDefinition(TypeDefinition type, out SagaDefinition definition)
    {
        var typeFullName = type.FullName;
        var attribute = type.GetSingleAttribute("NServiceBus.Persistence.Sql.SqlSagaAttribute");
        if (attribute == null)
        {
            if (!type.IsAbstract && type.BaseType != null)
            {
                var baseTypeFullName = type.BaseType.FullName;
                if (baseTypeFullName.StartsWith("NServiceBus.Saga"))
                {
                    throw new ErrorsException($"The type '{typeFullName}' inherits from NServiceBus.Saga but is missing a [SqlSagaAttribute].");
                }
                if (baseTypeFullName.StartsWith("NServiceBus.Persistence.Sql.SqlSaga"))
                {
                    throw new ErrorsException($"The type '{typeFullName}' inherits from NServiceBus.Persistence.Sql.SqlSaga but is missing a [SqlSagaAttribute].");
                }
            }
            definition = null;
            return false;
        }
        if (type.HasGenericParameters)
        {
            throw new ErrorsException($"The type '{typeFullName}' has a [SqlSagaAttribute] but has generic parameters.");
        }
        if (type.IsAbstract)
        {
            throw new ErrorsException($"The type '{typeFullName}' has a [SqlSagaAttribute] but has is abstract.");
        }

        var arguments = attribute.ConstructorArguments;
        var correlationArgumentValue = (string)arguments[0].Value;
        var transitionalArgumentValue = (string)arguments[1].Value;
        var tableSuffixArgumentValue = (string)arguments[2].Value;
        SagaDefinitionValidator.ValidateSagaDefinition(correlationArgumentValue, typeFullName, transitionalArgumentValue, tableSuffixArgumentValue);

        string tableSuffix;

        if (tableSuffixArgumentValue == null)
        {
            tableSuffix = type.Name;
        }
        else
        {
            tableSuffix = tableSuffixArgumentValue;
        }

        var sagaDataType = GetSagaDataTypeFromSagaType(type);

        definition = new SagaDefinition
        (
            correlationProperty: BuildConstraintMember(sagaDataType, correlationArgumentValue),
            transitionalCorrelationProperty: BuildConstraintMember(sagaDataType, transitionalArgumentValue),
            tableSuffix: tableSuffix,
            name: type.FullName
        );
        return true;
    }

   
    static TypeDefinition GetSagaDataTypeFromSagaType(TypeDefinition sagaType)
    {
        foreach (var method in sagaType.Methods)
        {
            if (method.Name != "ConfigureHowToFindSaga")
            {
                continue;
            }
            if (method.Parameters.Count != 1)
            {
                continue;
            }

            var parameterType = method.Parameters[0].ParameterType;
            var parameterTypeName = parameterType.Name;
            if (!parameterTypeName.StartsWith("SagaPropertyMapper"))
            {
                continue;
            }
            var genericInstanceType = (GenericInstanceType)parameterType;
            var argument = genericInstanceType.GenericArguments.Single();
            var sagaDataType = argument as TypeDefinition;
            if (sagaDataType == null)
            {
                throw new ErrorsException($"The type '{sagaType.FullName}' uses a sagadata type not defined in the same assembly.");
            }
            return sagaDataType;

        }
        throw new ErrorsException($"The type '{sagaType.FullName}' needs to override ConfigureHowToFindSaga(SagaPropertyMapper).");
    }

    static CorrelationProperty BuildConstraintMember(TypeDefinition sagaDataTypeDefinition, string propertyName)
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

        //todo: verify not readonly
        return BuildConstraintMember(propertyDefinition);
    }

    static CorrelationProperty BuildConstraintMember(PropertyDefinition member)
    {
        return new CorrelationProperty
        {
            Name = member.Name,
            Type = GetConstraintMemberType(member)
        };
    }

    static CorrelationMemberType GetConstraintMemberType(PropertyDefinition propertyDefinition)
    {
        var memberType = propertyDefinition.PropertyType;
        var fullName = memberType.FullName;
        if (
            fullName == typeof(short).FullName ||
            fullName == typeof(int).FullName ||
            fullName == typeof(long).FullName ||
            fullName == typeof(ushort).FullName ||
            fullName == typeof(uint).FullName
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
        if (fullName == typeof(string).FullName)
        {
            return CorrelationMemberType.String;
        }
        throw new ErrorsException($"Could not convert '{fullName}' to {typeof(CorrelationMemberType).Name}.");
    }
}
