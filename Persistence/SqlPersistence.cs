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
                s.SetTimeoutIsEnabled();
            });
            Supports<StorageType.Sagas>(s =>
            {
                s.EnableFeatureByDefault<SagaFeature>();
                s.SetSagaIsEnabled();
            });
            Supports<StorageType.Subscriptions>(s =>
            {
                s.EnableFeatureByDefault<SubscriptionFeature>();
                s.SetSubscriptionIsEnabled();
            });
        }
        
    }
}