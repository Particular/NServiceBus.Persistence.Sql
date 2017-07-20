using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

namespace NServiceBus
{
    using Features;

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
            return settings.TryGet("SqlPersistence.DisableInstaller", out bool value) && value;
        }

        internal static bool ShouldInstall<TFeature>(this ReadOnlySettings settings)
            where TFeature : Feature
        {
            var featureEnabled = settings.IsFeatureActive(typeof(TFeature));
            var disableInstaller = settings.GetDisableInstaller();
            return
                featureEnabled &&
                !disableInstaller;
        }


    }
}