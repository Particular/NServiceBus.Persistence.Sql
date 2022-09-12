namespace NServiceBus.TransactionalSession
{
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
            this PersistenceExtensions<SqlPersistence> persistenceExtensions)
        {
            persistenceExtensions.GetSettings().EnableFeatureByDefault<SqlPersistenceTransactionalSession>();
            return persistenceExtensions;
        }
    }
}