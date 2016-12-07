using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{

    public static partial class SqlPersistenceConfig
    {

        public static void UseEndpointName(this PersistenceExtensions<SqlPersistence> configuration, bool useEndpointName)
        {
            configuration.GetSettings()
                .Set("SqlPersistence.UseEndpointName", useEndpointName);
        }

        public static void UseEndpointName<TStorageType>(this PersistenceExtensions<SqlPersistence, TStorageType> configuration, bool useEndpointName)
            where TStorageType : StorageType
        {
            var key = $"SqlPersistence.{typeof(TStorageType).Name}.UseEndpointName";
            configuration.GetSettings()
                .Set(key, useEndpointName);
        }

        static bool ShouldUseEndpointName<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<bool, TStorageType>("UseEndpointName", () => true);
        }

        internal static string GetTablePrefixForEndpoint<T>(this ReadOnlySettings settings) where T : StorageType
        {
            if (settings.ShouldUseEndpointName<T>())
            {
                return $"{settings.EndpointName()}.";
            }
            return "";
        }

    }

}