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
            CorrelationId = attribute.CorrelationId,
            TransitionalCorrelationId = attribute.TransitionalCorrelationId
        };
    }
}

class SqlSagaAttributeData
{
    public string TableSuffix;
    public string CorrelationId;
    public string TransitionalCorrelationId;
}