using System;
using System.Reflection;
using NServiceBus.Persistence.Sql;

static class SqlSagaAttributeReader
{
    public static SqlSagaAttributeData GetSqlSagaAttributeData(Type sagaType)
    {
        var correlated = sagaType.GetCustomAttribute<CorrelatedSagaAttribute>(false);
        if (correlated != null)
        {
            var tableName = correlated.TableSuffix;
            if (tableName == null)
            {
                tableName = sagaType.Name;
            }
            return new SqlSagaAttributeData
            {
                TableSuffix = tableName,
                CorrelationProperty = correlated.CorrelationProperty,
                TransitionalCorrelationProperty = correlated.TransitionalCorrelationProperty
            };
        }
        var alwaysNew = sagaType.GetCustomAttribute<AlwaysStartNewSagaAttribute>(false);
        if (alwaysNew != null)
        {
            var tableName = alwaysNew.TableSuffix;
            if (tableName == null)
            {
                tableName = sagaType.Name;
            }
            return new SqlSagaAttributeData
            {
                AlwaysStartNew = true,
                TableSuffix = tableName,
            };
        }
        throw new Exception($"Expected to find a either [{nameof(CorrelatedSagaAttribute)}] or [{nameof(AlwaysStartNewSagaAttribute)}] on saga '{sagaType.FullName}'.");
    }
}