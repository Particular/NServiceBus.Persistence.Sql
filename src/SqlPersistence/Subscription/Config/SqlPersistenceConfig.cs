namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Persistence.Sql;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Exposes subscription specific settings.
        /// </summary>
        public static SubscriptionSettings SubscriptionSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            return new SubscriptionSettings(configuration.GetSettings());
        }
    }
}