namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Persistence.Sql;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Disables the SQL persistence installers.
        /// </summary>
        public static void DisableInstaller(this PersistenceExtensions<SqlPersistence> configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);

            var outboxInstallerSettings = configuration.GetSettings().GetOrCreate<SqlOutboxInstallerSettings>();
            outboxInstallerSettings.Disabled = true;

            var sagaInstallerSettings = configuration.GetSettings().GetOrCreate<SqlSagaInstallerSettings>();
            sagaInstallerSettings.Disabled = true;

            var subscriptionInstallerSettings = configuration.GetSettings().GetOrCreate<SqlSubscriptionInstallerSettings>();
            subscriptionInstallerSettings.Disabled = true;

            var timeoutInstallerSettings = configuration.GetSettings().GetOrCreate<SqlTimeoutInstallerSettings>();
            timeoutInstallerSettings.Disabled = true;
        }
    }
}