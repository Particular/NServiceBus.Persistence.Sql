using NServiceBus.Features;

namespace NServiceBus.Persistence.Sql
{

    public class SqlXmlPersistence : PersistenceDefinition
    {
        public SqlXmlPersistence()
        {
            Defaults(s =>
            {
                // always enable these ones since they will only enable if the outbox or sagas are on
                s.EnableFeatureByDefault<StorageSessionFeature>();
                s.Set<EnabledStorageFeatures>(new EnabledStorageFeatures());
            });
            Supports<StorageType.Outbox>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlOutboxFeature>();
            });
            Supports<StorageType.Timeouts>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlTimeoutFeature>();
            });
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlSagaFeature>();
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlSubscriptionFeature>();
            });
        }
    }
}