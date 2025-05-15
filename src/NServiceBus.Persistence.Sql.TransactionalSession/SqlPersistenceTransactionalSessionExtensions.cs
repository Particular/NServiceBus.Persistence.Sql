namespace NServiceBus.TransactionalSession
{
    using System;
    using Configuration.AdvancedExtensibility;
    using Features;

    /// <summary>
    /// Enables the transactional session feature.
    /// </summary>
    public static class SqlPersistenceTransactionalSessionExtensions
    {
        /// <summary>
        /// Enables transactional session for this endpoint.
        /// </summary>
        public static PersistenceExtensions<SqlPersistence> EnableTransactionalSession(
            this PersistenceExtensions<SqlPersistence> persistenceExtensions) =>
            EnableTransactionalSession(persistenceExtensions, new TransactionalSessionOptions());

        /// <summary>
        /// Enables the transactional session for this endpoint using the specified TransactionalSessionOptions.
        /// </summary>
        public static PersistenceExtensions<SqlPersistence> EnableTransactionalSession(this PersistenceExtensions<SqlPersistence> persistenceExtensions,
            TransactionalSessionOptions transactionalSessionOptions)
        {
            ArgumentNullException.ThrowIfNull(persistenceExtensions);
            ArgumentNullException.ThrowIfNull(transactionalSessionOptions);

            var settings = persistenceExtensions.GetSettings();

            settings.Set(transactionalSessionOptions);
            settings.EnableFeatureByDefault<SqlPersistenceTransactionalSession>();

            if (!string.IsNullOrEmpty(transactionalSessionOptions.ProcessorEndpoint))
            {
                settings.Set(SqlOutboxFeature.ProcessorEndpointKey, transactionalSessionOptions.ProcessorEndpoint);

                // remote processor configured so turn off the outbox cleanup on this instance
                settings.Set(SqlOutboxFeature.DisableCleanup, true);
            }

            return persistenceExtensions;
        }
    }
}