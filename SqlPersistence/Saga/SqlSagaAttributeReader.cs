using System;
using System.Reflection;
using NServiceBus.Persistence.Sql;

static class SqlSagaAttributeReader
{
    public static SqlSagaAttributeData GetSqlSagaAttributeData(Type sagaType)
    {
        var attribute = sagaType.GetCustomAttribute<SqlSagaAttribute>(false);
        if (attribute == null)
        {
            throw new Exception($"Expected to find a [{nameof(SqlSagaAttribute)}] on saga '{sagaType.FullName}'.");
        }
        var tableName = attribute.TableSuffix;
        if (tableName == null)
        {
            tableName = sagaType.Name;
        }
        return new SqlSagaAttributeData
        {
            TableSuffix = tableName,
            CorrelationProperty = attribute.CorrelationProperty,
            TransitionalCorrelationProperty = attribute.TransitionalCorrelationProperty
        };
    }
}