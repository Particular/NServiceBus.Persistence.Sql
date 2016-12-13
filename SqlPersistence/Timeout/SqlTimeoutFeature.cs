using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Timeout.Core;

class SqlTimeoutFeature : Feature
{

    SqlTimeoutFeature()
    {
        DependsOn<TimeoutManager>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        settings.EnableFeature<StorageType.Timeouts>();
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Timeouts>();
        var endpointName = settings.GetTablePrefix<StorageType.Timeouts>();
        var persister = new TimeoutPersister(connectionBuilder, endpointName);
        context.Container.RegisterSingleton(typeof(IPersistTimeouts), persister);
        context.Container.RegisterSingleton(typeof(IQueryTimeouts), persister);
    }
}