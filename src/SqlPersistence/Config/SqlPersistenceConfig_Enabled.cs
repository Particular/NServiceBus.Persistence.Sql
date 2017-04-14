using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Disables the SQL persistence installers.
        /// </summary>
        public static void DisableInstaller(this PersistenceExtensions<SqlPersistence> configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            configuration.GetSettings()
                .Set("SqlPersistence.DisableInstaller", true);
        }

        static bool GetDisableInstaller(this ReadOnlySettings settings)
        {
            bool value;
            if (settings.TryGet("SqlPersistence.DisableInstaller", out value))
            {
                return value;
            }
            return false;
        }

        internal static void EnableFeature<TStorageType>(this ReadOnlySettings settingsHolder)
            where TStorageType : StorageType
        {
            settingsHolder.Get<EnabledStorageFeatures>().Enable<TStorageType>();
        }

        static bool GetFeatureEnabled<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.GetOrDefault<EnabledStorageFeatures>()?.IsEnabled<TStorageType>() ?? false;
        }

        internal static bool ShouldInstall<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            var featureEnabled = settings.GetFeatureEnabled<TStorageType>();
            var disableInstaller = settings.GetDisableInstaller();
            return
                featureEnabled &&
                !disableInstaller;
        }


    }
}