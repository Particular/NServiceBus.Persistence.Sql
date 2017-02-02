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
        var sqlVariant = settings.GetSqlVariant();
        var connectionBuilder = settings.GetConnectionBuilder();
        var endpointName = settings.GetTablePrefix();
        var persister = new TimeoutPersister(connectionBuilder, endpointName, sqlVariant);
        context.Container.RegisterSingleton(typeof(IPersistTimeouts), persister);
        context.Container.RegisterSingleton(typeof(IQueryTimeouts), persister);
    }
}