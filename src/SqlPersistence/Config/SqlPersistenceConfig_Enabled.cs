using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Persistence.Sql;

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

            var installerSettings = configuration.GetSettings().GetOrCreate<InstallerSettings>();
            installerSettings.Disabled = true;
        }
    }
}