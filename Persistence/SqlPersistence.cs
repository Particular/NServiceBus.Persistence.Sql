using NServiceBus.Features;

namespace NServiceBus.Persistence
{
    public class SqlPersistence : PersistenceDefinition
    {
        public SqlPersistence()
        {
            Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<TimeoutFeature>());
            Supports<StorageType.Sagas>(s => s.EnableFeatureByDefault<SagaFeature>());
            Supports<StorageType.Subscriptions>(s => s.EnableFeatureByDefault<SubscriptionFeature>());
        }
    }
}