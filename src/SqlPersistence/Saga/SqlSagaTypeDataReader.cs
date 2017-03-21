using System;
using System.Reflection;
using System.Runtime.Serialization;
using NServiceBus.Persistence.Sql;

static class SqlSagaTypeDataReader
{
    public static SqlSagaTypeData GetTypeData(Type sagaType)
    {
        var attribute = sagaType.GetCustomAttribute<SqlSagaAttribute>(false);
        string tableName = null;
        string transitionalCorrelationProperty = null;
        if (attribute != null)
        {
            tableName = attribute.TableSuffix;
            transitionalCorrelationProperty = attribute.TransitionalCorrelationProperty;
        }
        if (tableName == null)
        {
            tableName = sagaType.Name;
        }
        var instance = FormatterServices.GetUninitializedObject(sagaType);
        var correlationProperty = GetCorrelationProperty(sagaType);
        correlationProperty.GetMethod.Invoke(instance, null);
        return new SqlSagaTypeData
        {
            TableSuffix = tableName,
            CorrelationProperty = (string) correlationProperty.GetMethod.Invoke(instance, null),
            TransitionalCorrelationProperty = transitionalCorrelationProperty
        };
    }

    static PropertyInfo GetCorrelationProperty(Type sagaType)
    {
        return sagaType.GetProperty("CorrelationPropertyName", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
    }
}