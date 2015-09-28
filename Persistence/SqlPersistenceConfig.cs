using System;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Settings;

namespace NServiceBus
{
    public static class SqlPersistenceConfig
    {
        public static void ConnectionString(this PersistenceExtentions<Persistence.SqlPersistence> persistenceConfiguration, string connectionString)
        {
            persistenceConfiguration.GetSettings()
                .Set("SqlPersistence.ConnectionString", connectionString);
        }

        internal static string GetConnectionString(this ReadOnlySettings settings)
        {
            var connectionString = settings.GetOrDefault<string>("SqlPersistence.ConnectionString");
            if (connectionString == null)
            {
                throw new Exception("ConnectionString must be defined.");
            }
            return connectionString;
        }

        public static void Schema(this PersistenceExtentions<Persistence.SqlPersistence> persistenceConfiguration, string schema)
        {
            persistenceConfiguration.GetSettings()
                .Set("SqlPersistence.Schema", schema);
        }

        internal static string GetSchema(this ReadOnlySettings settings)
        {
            var schema = settings.GetOrDefault<string>("SqlPersistence.Schema");
            if (schema == null)
            {
                return "dbo";
            }
            return schema;
        }
    }
}