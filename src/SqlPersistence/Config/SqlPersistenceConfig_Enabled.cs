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
            if (settings.TryGet("SqlPersistence.DisableInstaller", out bool value))
            {
                return value;
            }
            return false;
        }

        internal static void EnableFeature<TStorageType>(this SettingsHolder settingsHolder)
            where TStorageType : StorageType
        {
            settingsHolder.GetOrCreate<EnabledStorageFeatures>().Enable<TStorageType>();
        }

        internal static bool GetFeatureEnabled<TStorageType>(this ReadOnlySettings settings)
            where TStorageType : StorageType
        {
            return settings.Get<EnabledStorageFeatures>().IsEnabled<TStorageType>();
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