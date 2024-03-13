namespace NServiceBus
{
    using System;
    using Configuration.AdvancedExtensibility;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Disables the SQL persistence installers.
        /// </summary>
        public static void DisableInstaller(this PersistenceExtensions<SqlPersistence> configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var installerSettings = configuration.GetSettings().GetOrCreate<InstallerSettings>();
            installerSettings.Disabled = true;
        }
    }
}