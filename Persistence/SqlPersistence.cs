using NServiceBus.Features;

namespace NServiceBus.Persistence
{
    public class SqlPersistence : PersistenceDefinition
    {
        public SqlPersistence()
        {
            Supports<StorageType.Timeouts>(s =>
            {
                s.EnableFeatureByDefault<TimeoutFeature>();
                s.EnableFeature<StorageType.Timeouts>();
            });
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<SagaFeature>();
                s.EnableFeature<StorageType.Sagas>();
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeatureByDefault<SubscriptionFeature>();
                s.EnableFeature<StorageType.Subscriptions>();
            });
            
        }
        
    }
}