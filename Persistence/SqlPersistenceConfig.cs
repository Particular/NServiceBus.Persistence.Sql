using System;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
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
        public static void ConnectionString<TStorageType>(this PersistenceExtentions<Persistence.SqlPersistence, TStorageType> persistenceConfiguration, string connectionString)
            where TStorageType : StorageType
        {
            var key = "SqlPersistence." + typeof(TStorageType).Name + ".ConnectionString";
            persistenceConfiguration.GetSettings()
                .Set(key, connectionString);
        }

        internal static string GetConnectionString<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            var key = "SqlPersistence." + typeof(TStorageType).Name + ".ConnectionString";
            var storageSchema = settings.GetOrDefault<string>(key);
            if (storageSchema != null)
            {
                return storageSchema;
            }
            var rootConnectionString = settings.GetOrDefault<string>("SqlPersistence.ConnectionString");
            if (rootConnectionString != null)
            {
                return rootConnectionString;
            }
            throw new Exception("ConnectionString must be defined.");
        }

        public static void Schema(this PersistenceExtentions<Persistence.SqlPersistence> persistenceConfiguration, string schema)
        {
            persistenceConfiguration.GetSettings()
                .Set("SqlPersistence.Schema", schema);
        }

        public static void Schema<TStorageType>(this PersistenceExtentions<Persistence.SqlPersistence, TStorageType> persistenceConfiguration, string schema) 
            where TStorageType : StorageType
        {
            var key = "SqlPersistence." + typeof(TStorageType).Name+ ".Schema";
            persistenceConfiguration.GetSettings()
                .Set(key, schema);
        }

        internal static string GetSchema<TStorageType>(this ReadOnlySettings settings)
        {
            var key = "SqlPersistence." + typeof(TStorageType).Name + ".Schema";
            var storageSchema = settings.GetOrDefault<string>(key);
            if (storageSchema != null)
            {
                return storageSchema;
            }
            var rootSchema = settings.GetOrDefault<string>("SqlPersistence.Schema");
            if (rootSchema != null)
            {
                return rootSchema;
            }
            return "dbo";
        }
    }
}