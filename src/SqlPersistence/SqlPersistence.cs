using NServiceBus.Features;

namespace NServiceBus.Persistence.Sql
{
    using Settings;

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
            Supports<StorageType.Outbox>(s =>
            {
                EnableSession(s);
                s.EnableFeatureByDefault<SqlOutboxFeature>();
            });
            Supports<StorageType.Timeouts>(s =>
            {
                s.EnableFeatureByDefault<SqlTimeoutFeature>();
            });
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<SqlSagaFeature>();
                EnableSession(s);
                s.AddUnrecoverableException(typeof(SerializationException));
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeatureByDefault<SqlSubscriptionFeature>();
            });
        }

        static void EnableSession(SettingsHolder s)
        {
            s.EnableFeatureByDefault<StorageSessionFeature>();
        }
    }
}