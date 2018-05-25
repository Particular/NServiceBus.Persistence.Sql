namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Persistence.Sql;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Exposes timeouts specific settings.
        /// </summary>
        public static TimeoutSettings TimeoutSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            return new TimeoutSettings(configuration.GetSettings());
        }
    }
}