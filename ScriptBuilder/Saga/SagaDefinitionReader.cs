using System;
using System.Linq;
using Mono.Cecil;
using NServiceBus.Persistence.Sql;

class SagaDefinitionReader
{

    public static bool TryGetSqlSagaDefinition(TypeDefinition sagaType, out SagaDefinition definition)
    {
        var attribute = sagaType.GetSingleAttribute("NServiceBus.Persistence.Sql.SqlSagaAttribute");
        if (attribute == null)
        {
            definition = null;
            return false;
        }
        if (sagaType.HasGenericParameters)
        {
            throw new ErrorsException($"The type '{sagaType.FullName}' has a [SqlSagaAttribute] but has generic parameters.");
        }
        if (sagaType.IsAbstract)
        {
            throw new ErrorsException($"The type '{sagaType.FullName}' has a [SqlSagaAttribute] but has is abstract.");
        }

        var correlationArgumentValue = (string)attribute.ConstructorArguments[0].Value;
        if (string.IsNullOrWhiteSpace(correlationArgumentValue))
        {
            throw new ErrorsException($"The type '{sagaType.FullName}' has a [SqlSagaAttribute] but a null or empty string is passed to correlationId.");
        }

        var transitionalArgumentValue = (string)attribute.ConstructorArguments[1].Value;
        if (transitionalArgumentValue != null && string.IsNullOrWhiteSpace(transitionalArgumentValue))
        {
            throw new ErrorsException($"The type '{sagaType.FullName}' has a [SqlSagaAttribute] but an empty string is passed to transitionalCorrelationId.");
        }

        var tableNameArgumentValue = (string)attribute.ConstructorArguments[2].Value;
        string tableName;
        if (tableNameArgumentValue == null)
        {
            tableName = sagaType.Name;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(tableNameArgumentValue))
            {
                throw new ErrorsException($"The type '{sagaType.FullName}' has a [SqlSagaAttribute] but an empty string is passed to tableName.");
            }
            tableName = tableNameArgumentValue;
        }


        if (correlationArgumentValue == transitionalArgumentValue)
        {
            throw new ErrorsException($"The type '{sagaType.FullName}' has a [SqlSagaAttribute] where the correlationId and transitionalCorrelationId are the same. Member: {correlationArgumentValue}");
        }

        var sagaDataType = GetSagaDataTypeFromSagaType(sagaType);

        var correlationMember = BuildConstraintMember(sagaDataType, correlationArgumentValue);
        CorrelationMember transitionalMember = null;
        if (transitionalArgumentValue != null)
        {
            transitionalMember = BuildConstraintMember(sagaDataType, transitionalArgumentValue);
        }
        definition = new SagaDefinition
        {
            CorrelationMember = correlationMember,
            TransitionalCorrelationMember = transitionalMember,
            Name = tableName
        };
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

    static CorrelationMember BuildConstraintMember(TypeDefinition sagaDataTypeDefinition, string propertyName)
    {
        var propertyDefinition = sagaDataTypeDefinition.Properties.SingleOrDefault(x => x.Name == propertyName);

        if (propertyDefinition == null)
        {
            throw new ErrorsException($"Expected type '{sagaDataTypeDefinition.FullName}' to contain a property named '{propertyName}'.");
        }

        //todo: verify not readonly
        return BuildConstraintMember(propertyDefinition);
    }

    static CorrelationMember BuildConstraintMember(PropertyDefinition member)
    {
        return new CorrelationMember
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
