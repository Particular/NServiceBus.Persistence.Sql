using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class StorageSessionFeature : Feature
{

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionString = context.Settings.GetConnectionString<StorageType.Sagas>();
        var container = context.Container;
        container.ConfigureComponent(b => new SynchronizedStorage(connectionString), DependencyLifecycle.SingleInstance);
        container.ConfigureComponent(b => new StorageAdapter(connectionString), DependencyLifecycle.SingleInstance);
    }

}