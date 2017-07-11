using System;
using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;
using NServiceBus.Persistence;

class StorageSessionFeature : Feature
{

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        var isSagasEnabled = settings.GetFeatureEnabled<StorageType.Sagas>();
        var isOutboxEnabled = settings.GetFeatureEnabled<StorageType.Outbox>();
        if (!isSagasEnabled || !isOutboxEnabled)
        {
            throw new Exception("Sql Persistence must be enable for either both Sagas and Outbox, or neither.");
        }
        var connectionBuilder = settings.GetConnectionBuilder();
        var container = context.Container;
        container.ConfigureComponent(builder => new SynchronizedStorage(connectionBuilder, GetInfoCache(builder)), DependencyLifecycle.SingleInstance);
        container.ConfigureComponent(builder => new StorageAdapter(connectionBuilder, GetInfoCache(builder)), DependencyLifecycle.SingleInstance);
    }

    static SagaInfoCache GetInfoCache(IBuilder builder)
    {
        return builder.BuildAll<SagaInfoCache>().SingleOrDefault();
    }
}