using NServiceBus.Features;

namespace NServiceBus.Persistence.Sql
{
    public class SqlPersistence : PersistenceDefinition
    {
        //TODO: throw for schema in mysql
        public SqlPersistence()
        {
            Defaults(s =>
            {
                // always enable these ones since they will only enable if the outbox or sagas are on
                s.EnableFeatureByDefault<StorageSessionFeature>();
                s.Set<EnabledStorageFeatures>(new EnabledStorageFeatures());
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
        }
    }
}