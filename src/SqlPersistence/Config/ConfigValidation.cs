using System;
using System.Text;
using NServiceBus;
using NServiceBus.Persistence.Sql;

static class ConfigValidation
{
    public static void ValidateTableSettings(SqlVariant variant, string tablePrefix, string schema)
    {
        if (variant == SqlVariant.Oracle)
        {
            if (tablePrefix.Length > 25)
            {
                throw new Exception($"Table prefix '{tablePrefix}' contains more than 25 characters, which is not supported by SQL persistence using Oracle. Shorten the endpoint name or specify a custom tablePrefix using endpointConfiguration.{nameof(SqlPersistenceConfig.TablePrefix)}(tablePrefix).");
            }
            if (Encoding.UTF8.GetBytes(tablePrefix).Length != tablePrefix.Length)
            {
                throw new Exception($"Table prefix '{tablePrefix}' contains non-ASCII characters, which is not supported by SQL persistence using Oracle. Change the endpoint name or specify a custom tablePrefix using endpointConfiguration.{nameof(SqlPersistenceConfig.TablePrefix)}(tablePrefix).");
            }
        }
    }
}

