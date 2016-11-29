using System;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    public static class SqlXmlPersistenceConfig
    {
        public static void ConnectionString(this PersistenceExtensions<SqlXmlPersistence> configuration, string connectionString)
        {
            configuration.GetSettings()
                .Set("SqlXmlPersistence.ConnectionString", connectionString);
        }

        public static void ConnectionString<TStorageType>(this PersistenceExtensions<SqlXmlPersistence, TStorageType> configuration, string connectionString)
            where TStorageType : StorageType
        {
            var key = $"SqlXmlPersistence.{typeof(TStorageType).Name}.ConnectionString";
            configuration.GetSettings()
                .Set(key, connectionString);
        }

        internal static string GetConnectionString<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<string, TStorageType>("ConnectionString", () =>
            {
                throw new Exception("ConnectionString must be defined.");
            });
        }

        public static void UseEndpointName(this PersistenceExtensions<SqlXmlPersistence> configuration, bool useEndpointName)
        {
            configuration.GetSettings()
                .Set("SqlXmlPersistence.UseEndpointName", useEndpointName);
        }

        public static void UseEndpointName<TStorageType>(this PersistenceExtensions<SqlXmlPersistence, TStorageType> configuration, bool useEndpointName)
            where TStorageType : StorageType
        {
            var key = $"SqlXmlPersistence.{typeof(TStorageType).Name}.UseEndpointName";
            configuration.GetSettings()
                .Set(key, useEndpointName);
        }

        static bool ShouldUseEndpointName<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<bool, TStorageType>("UseEndpointName", () => true);
        }

        internal static string GetEndpointNamePrefix<TStorageType>(this ReadOnlySettings settings) 
            where TStorageType : StorageType
        {
            if (settings.ShouldUseEndpointName<TStorageType>())
            {
                return $"{settings.EndpointName()}.";
            }
            return "";
        }


        public static void DisableInstaller(this PersistenceExtensions<SqlXmlPersistence> configuration)
        {
            configuration.GetSettings()
                .Set("SqlXmlPersistence.DisableInstaller", true);
        }

        public static void DisableInstaller<TStorageType>(this PersistenceExtensions<SqlXmlPersistence, TStorageType> configuration)
            where TStorageType : StorageType
        {
            var key = $"SqlXmlPersistence.{typeof(TStorageType).Name}.DisableInstaller";
            configuration.GetSettings()
                .Set(key, true);
        }

        internal static bool GetDisableInstaller<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetValue<bool, TStorageType>("DisableInstaller", () => false);
        }

        internal static void EnableFeature<TStorageType>(this ReadOnlySettings settingsHolder)
            where TStorageType : StorageType
        {
            settingsHolder.Get<EnabledStorageFeatures>().Enable<TStorageType>();
        }

        internal static bool GetFeatureEnabled<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetOrDefault<EnabledStorageFeatures>()?.IsEnabled<TStorageType>() ?? false;
        }

        public static bool ShouldInstall<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            var featureEnabled = settings.GetFeatureEnabled<TStorageType>();
            var disableInstaller = settings.GetDisableInstaller<TStorageType>();
            return
                featureEnabled &&
                !disableInstaller;
        }

        public static void Schema(this PersistenceExtensions<SqlXmlPersistence> configuration, string schema)
        {
            configuration.GetSettings()
                .Set("SqlXmlPersistence.Schema", schema);
        }

        public static void Schema<TStorageType>(this PersistenceExtensions<SqlXmlPersistence, TStorageType> configuration, string schema)
            where TStorageType : StorageType
        {
            var key = $"SqlXmlPersistence.{typeof(TStorageType).Name}.Schema";
            configuration.GetSettings()
                .Set(key, schema);
        }

        internal static string GetSchema<TStorageType>(this ReadOnlySettings settings) where TStorageType : StorageType
        {
            return settings.GetValue<string, TStorageType>("Schema", () => "dbo");
        }

        internal static TValue GetValue<TValue, TStorageType>(this ReadOnlySettings settings, string suffix, Func<TValue> defaultValue)
            where TStorageType : StorageType
        {
            var key = $"SqlXmlPersistence.{typeof (TStorageType).Name}.{suffix}";
            TValue value;
            if (settings.TryGet(key, out value))
            {
                return value;
            }
            if (settings.TryGet($"SqlXmlPersistence.{suffix}", out value))
            {
                return value;
            }
            return defaultValue();
        }
    }
}