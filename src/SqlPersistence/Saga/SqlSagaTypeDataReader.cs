using System;
using System.Linq;
using System.Reflection;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;

static class SqlSagaTypeDataReader
{
    public static SqlSagaTypeData GetTypeData(SagaMetadata metadata)
    {
        var sagaType = metadata.SagaType;

        if (sagaType.IsSubclassOfRawGeneric(typeof(NServiceBus.Saga<>)))
        {
            return GetTypeDataFromCoreSaga(metadata);
        }

        throw new Exception($"Type '{sagaType.FullName}' is not a Saga<T>.");
    }

    static SqlSagaTypeData GetTypeDataFromCoreSaga(SagaMetadata metadata)
    {
        var attribute = metadata.SagaType.GetCustomAttributes().OfType<SqlSagaAttribute>().FirstOrDefault();

        var correlationProperty = attribute?.CorrelationProperty;

        if (correlationProperty == null)
        {
            if (metadata.TryGetCorrelationProperty(out var property))
            {
                correlationProperty = property.Name;
            }
        }

        return new SqlSagaTypeData
        {
            TableSuffix = attribute?.TableSuffix ?? metadata.SagaType.Name,
            CorrelationProperty = correlationProperty,
            TransitionalCorrelationProperty = attribute?.TransitionalCorrelationProperty
        };
    }
}