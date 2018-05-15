namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Persistence.Sql;

    public static partial class SqlPersistenceConfig
    {
        /// <summary>
        /// Exposes saga specific settings.
        /// </summary>
        public static SagaSettings SagaSettings(this PersistenceExtensions<SqlPersistence> configuration)
        {
            return new SagaSettings(configuration.GetSettings());
        }
    }
}