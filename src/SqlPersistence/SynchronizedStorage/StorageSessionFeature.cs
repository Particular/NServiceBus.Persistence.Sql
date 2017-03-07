using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;

class StorageSessionFeature : Feature
{

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionBuilder = context.Settings.GetConnectionBuilder();
        var container = context.Container;
        container.ConfigureComponent(builder => new SynchronizedStorage(connectionBuilder, GetInfoCache(builder)), DependencyLifecycle.SingleInstance);
        container.ConfigureComponent(builder => new StorageAdapter(GetInfoCache(builder)), DependencyLifecycle.SingleInstance);
    }

    static SagaInfoCache GetInfoCache(IBuilder builder)
    {
        return builder.BuildAll<SagaInfoCache>().SingleOrDefault();
    }
}