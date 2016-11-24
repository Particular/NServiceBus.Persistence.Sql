using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Timeout.Core;

class SqlXmlTimeoutFeature : Feature
{

    SqlXmlTimeoutFeature()
    {
        DependsOn<TimeoutManager>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Timeouts>();
        var connectionString = context.Settings.GetConnectionString<StorageType.Timeouts>();
        var schema = context.Settings.GetSchema<StorageType.Timeouts>();
        var endpointName = context.Settings.ShouldUseEndpointName<StorageType.Timeouts>()
            ? context.Settings.EndpointName() + "."
            : "";

        var persister = new TimeoutPersister(connectionString, schema, endpointName);
        context.Container.RegisterSingleton(typeof (IPersistTimeouts), persister);
        context.Container.RegisterSingleton(typeof (IQueryTimeouts), persister);
    }
}