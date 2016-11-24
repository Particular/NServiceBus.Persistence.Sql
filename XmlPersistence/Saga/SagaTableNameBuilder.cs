using System;
using System.Reflection;
using NServiceBus.Persistence.Sql.Xml;

static class SagaTableNameBuilder
{
    public static string GetTableSuffix(Type sagaType)
    {
        var attribute = sagaType.GetCustomAttribute<SqlSagaAttribute>(false);
        if (attribute == null)
        {
            throw new Exception($"Expected to find a [{nameof(SqlSagaAttribute)}] on saga '{sagaType.FullName}'.");
        }
        var tableName = attribute.TableName;
        if (tableName == null)
        {
            return sagaType.Name;
        }
        return tableName;
    }
}