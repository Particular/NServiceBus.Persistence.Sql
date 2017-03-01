using NServiceBus;
using NServiceBus.Features;

class StorageSessionFeature : Feature
{

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionBuilder = context.Settings.GetConnectionBuilder();
        var container = context.Container;
        
        container.ConfigureComponent(b => new SynchronizedStorage(connectionBuilder, b.Build<SagaInfoCache>()), DependencyLifecycle.SingleInstance);
        container.ConfigureComponent(b => new StorageAdapter(b.Build<SagaInfoCache>()), DependencyLifecycle.SingleInstance);
    }

}