using NServiceBus.Features;

namespace NServiceBus.Persistence.SqlServerXml
{
    public class SqlXmlPersistence : PersistenceDefinition
    {
        public SqlXmlPersistence()
        {
            Defaults(s =>
            {
                //we can always enable these ones since they will only enable if the outbox or sagas are on
                s.EnableFeatureByDefault<StorageSessionFeature>();
            });
            Supports<StorageType.Outbox>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlOutboxFeature>();
                s.EnableFeature<StorageType.Outbox>();
            });
            Supports<StorageType.Timeouts>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlTimeoutFeature>();
                s.EnableFeature<StorageType.Timeouts>();
            });
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlSagaFeature>();
                s.EnableFeature<StorageType.Sagas>();
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeatureByDefault<SqlXmlSubscriptionFeature>();
                s.EnableFeature<StorageType.Subscriptions>();
            });
        }
    }
}