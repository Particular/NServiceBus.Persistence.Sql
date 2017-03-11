using System;
using System.Reflection;
using NServiceBus.Persistence.Sql;

static class SqlSagaAttributeReader
{
    public static SqlSagaAttributeData GetSqlSagaAttributeData(Type sagaType)
    {
        var correlated = sagaType.GetCustomAttribute<CorrelatedSagaAttribute>(false);
        var alwaysNew = sagaType.GetCustomAttribute<AlwaysStartNewSagaAttribute>(false);

        if (alwaysNew != null && correlated != null)
        {
            throw new Exception($"The type '{sagaType.FullName}' contains both a [{nameof(CorrelatedSagaAttribute)}] and [{nameof(AlwaysStartNewSagaAttribute)}].");
        }

        if (correlated != null)
        {
            var tableName = GetTableName(sagaType, correlated.TableSuffix);
            if (string.IsNullOrWhiteSpace(correlated.CorrelationProperty))
            {
                throw new Exception($"The type '{nameof(sagaType.FullName)}' contains a {nameof(CorrelatedSagaAttribute)} with a null or empty correlationProperty parameter.");
            }
            if (correlated.TransitionalCorrelationProperty != null && string.IsNullOrWhiteSpace(correlated.TransitionalCorrelationProperty))
            {
                throw new Exception($"The type '{nameof(sagaType.FullName)}' contains a {nameof(CorrelatedSagaAttribute)} with an empty TransitionalCorrelationProperty property.");
            }
            return new SqlSagaAttributeData
            {
                TableSuffix = tableName,
                CorrelationProperty = correlated.CorrelationProperty,
                TransitionalCorrelationProperty = correlated.TransitionalCorrelationProperty
            };
        }
        if (alwaysNew != null)
        {
            var tableName = GetTableName(sagaType, alwaysNew.TableSuffix);
            return new SqlSagaAttributeData
            {
                AlwaysStartNew = true,
                TableSuffix = tableName,
            };
        }
        throw new Exception($"Expected to find a either [{nameof(CorrelatedSagaAttribute)}] or [{nameof(AlwaysStartNewSagaAttribute)}] on saga '{sagaType.FullName}'.");
    }

    static string GetTableName(Type sagaType, string alwaysNewTableSuffix)
    {
        var tableName = alwaysNewTableSuffix;
        if (tableName == null)
        {
            return sagaType.Name;
        }
        if (string.IsNullOrWhiteSpace(alwaysNewTableSuffix))
        {
            throw new Exception($"The type '{nameof(Type.FullName)}' contains a {nameof(CorrelatedSagaAttribute)} with an empty TableSuffix property.");
        }
        return tableName;
    }
}