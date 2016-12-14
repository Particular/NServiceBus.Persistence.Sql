using System;
using System.Data.Common;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    
    public static partial class SqlPersistenceConfig
    {
        public static void ConnectionBuilder(this PersistenceExtensions<SqlPersistence> configuration, Func<DbConnection> connectionBuilder)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.ConnectionBuilder", connectionBuilder);
        }

        public static void ConnectionBuilder<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, Func<DbConnection> connectionBuilder)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.ConnectionBuilder";
            configuration.GetSettings()
                .Set(key, connectionBuilder);
        }

        internal static Func<DbConnection> GetConnectionBuilder<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<Func<DbConnection>, TStorageType>("ConnectionBuilder",
                defaultValue: () =>
                {
                    throw new Exception("ConnectionBuilder must be defined.");
                });
        }

    }
}