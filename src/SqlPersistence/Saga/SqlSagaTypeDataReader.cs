using System;
using System.Reflection;
using System.Runtime.Serialization;

static class SqlSagaTypeDataReader
{
    public static SqlSagaTypeData GetTypeData(Type sagaType)
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