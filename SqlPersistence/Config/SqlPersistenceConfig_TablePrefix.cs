using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{

    public static partial class SqlPersistenceConfig
    {
        public static void TablePrefix(this PersistenceExtensions<SqlPersistence> configuration, string tablePrefix)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.TablePrefix", tablePrefix);
        }

        public static void TablePrefix<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, string tablePrefix)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.TablePrefix";
            configuration.GetSettings()
                .Set(key, tablePrefix);
        }

       
        internal static string GetTablePrefix<T>(this ReadOnlySettings settings) where T : StorageType
        {
            var value = settings.GetValue<string, T>("TablePrefix", () => null);
            if (value == null)
            {
                var endpointName = settings.EndpointName();
                var clean = TableNameCleaner.Clean(endpointName);
                return $"{clean}_";
            }
            return value;
        }

    }
}