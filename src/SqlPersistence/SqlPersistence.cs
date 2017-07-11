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
                s.EnableFeature<StorageType.Outbox>();
                EnableSession(s);
                s.EnableFeatureByDefault<SqlOutboxFeature>();
            });
            Supports<StorageType.Timeouts>(s =>
            {
                s.EnableFeature<StorageType.Timeouts>();
                s.EnableFeatureByDefault<SqlTimeoutFeature>();
            });
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeature<StorageType.Sagas>();
                s.EnableFeatureByDefault<SqlSagaFeature>();
                EnableSession(s);
                s.AddUnrecoverableException(typeof(SerializationException));
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeature<StorageType.Subscriptions>();
                s.EnableFeatureByDefault<SqlSubscriptionFeature>();
            });
        }

        static void EnableSession(SettingsHolder s)
        {
            s.EnableFeatureByDefault<StorageSessionFeature>();
        }
    }
}