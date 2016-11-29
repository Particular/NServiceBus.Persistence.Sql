using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class StorageSessionFeature : Feature
{

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionBuilder = context.Settings.GetConnectionBuilder<StorageType.Sagas>();
        var container = context.Container;
        container.ConfigureComponent(b => new SynchronizedStorage(connectionBuilder), DependencyLifecycle.SingleInstance);
        container.ConfigureComponent(b => new StorageAdapter(connectionBuilder), DependencyLifecycle.SingleInstance);
    }

}