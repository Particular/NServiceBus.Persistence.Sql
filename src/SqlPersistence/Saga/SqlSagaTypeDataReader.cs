using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;

static class SqlSagaTypeDataReader
{
    public static SqlSagaTypeData GetTypeData(SagaMetadata metadata)
    {
        var sagaType = metadata.SagaType;

        if (sagaType.IsSubclassOfRawGeneric(typeof(SqlSaga<>)))
        {
            return GetTypeDataFromSqlSaga(sagaType);
        }

        if (sagaType.IsSubclassOfRawGeneric(typeof(NServiceBus.Saga<>)))
        {
            return GetTypeDataFromCoreSaga(metadata);
        }

        throw new Exception($"Type '{sagaType.FullName}' is not a Saga<T>.");
    }

    static SqlSagaTypeData GetTypeDataFromSqlSaga(Type sagaType)
    {     
        var instance = FormatterServices.GetUninitializedObject(sagaType);

        string transitionalCorrelationPropertyName = null;
        var transitionalCorrelationProperty = GetProperty(sagaType, "TransitionalCorrelationPropertyName");
        if (transitionalCorrelationProperty != null)
        {
            transitionalCorrelationPropertyName = GetPropertyValue(transitionalCorrelationProperty, instance);
        }

        string tableName;
        var tableNameProperty = GetProperty(sagaType, "TableSuffix");
        if (tableNameProperty == null)
        {
            tableName = sagaType.Name;
        }
        else
        {
            tableName = GetPropertyValue(tableNameProperty, instance);
        }

        var correlationProperty = GetProperty(sagaType, "CorrelationPropertyName");
        return new SqlSagaTypeData
        {
            TableSuffix = tableName,
            CorrelationProperty = GetPropertyValue(correlationProperty, instance),
            TransitionalCorrelationProperty = transitionalCorrelationPropertyName
        };
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

    static string GetPropertyValue(PropertyInfo property, object instance)
    {
        return (string) property.GetMethod.Invoke(instance, null);
    }

    static PropertyInfo GetProperty(Type sagaType, string propertyName)
    {
        var propertyInfo = sagaType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
        if (propertyInfo == null)
        {
            return null;
        }
        if (propertyInfo.DeclaringType != sagaType)
        {
            return null;
        }
        return propertyInfo;
    }
}