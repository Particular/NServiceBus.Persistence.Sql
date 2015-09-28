using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Timeout.Core;


class TimeoutFeature : Feature
{

    TimeoutFeature()
    {
        DependsOn<TimeoutManager>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionString = context.Settings.GetConnectionString<StorageType.Timeouts>();
        var schema = context.Settings.GetSchema<StorageType.Timeouts>();
        var endpointName = context.Settings.EndpointName();
        var persister = new TimeoutPersister(connectionString, schema, endpointName);
        context.Container.RegisterSingleton(typeof (IPersistTimeouts), persister);
    }
}