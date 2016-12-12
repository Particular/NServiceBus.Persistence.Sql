using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    
    public static partial class SqlPersistenceConfig
    {

        public static void Schema(this PersistenceExtensions<SqlPersistence> configuration, string schema)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.Schema", schema);
        }

        public static void Schema<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, string schema)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.Schema";
            configuration.GetSettings()
                .Set(key, schema);
        }

        internal static string GetSchema<TStorageType>(this ReadOnlySettings settings) where TStorageType : StorageType
        {
            return settings.GetValue<string, TStorageType>("Schema", () => null);
        }

    }
}