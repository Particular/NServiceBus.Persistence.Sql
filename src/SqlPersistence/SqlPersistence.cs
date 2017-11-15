using NServiceBus.Features;

namespace NServiceBus.Persistence.Sql
{
    /// <summary>
    /// The <see cref="PersistenceDefinition"/> for the SQL Persistence.
    /// </summary>
    public class SqlPersistence : PersistenceDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SqlPersistence"/>.
        /// </summary>
        public SqlPersistence()
        {
            Defaults(s =>
            {
                // always enable these ones since they will only enable if the outbox or sagas are on
                s.EnableFeatureByDefault<StorageSessionFeature>();
                s.Set<EnabledStorageFeatures>(new EnabledStorageFeatures());
                s.AddUnrecoverableException(typeof(SerializationException));
            });
            Supports<StorageType.Outbox>(s =>
            {
                s.EnableFeatureByDefault<SqlOutboxFeature>();
            });
            Supports<StorageType.Timeouts>(s =>
            {
                s.EnableFeatureByDefault<SqlTimeoutFeature>();
            });
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<SqlSagaFeature>();
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeatureByDefault<SqlSubscriptionFeature>();
            });
            Defaults(s =>
            {
                s.EnableFeatureByDefault<InstallerFeature>();
            });
        }
    }
}