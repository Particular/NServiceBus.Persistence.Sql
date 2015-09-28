using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Timeout.Core;


class TimeoutFeature : Feature
{

    TimeoutFeature()
    {
        DependsOn<TimeoutManager>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionString = context.Settings.GetConnectionString();
        var schema = context.Settings.GetSchema();
        var endpointName = context.Settings.EndpointName();
        var persister = new TimeoutPersister(connectionString, schema, endpointName);
        context.Container.RegisterSingleton(typeof(IPersistTimeouts), persister);
    }
}