using NServiceBus;
using NServiceBus.Features;


class TimeoutFeature : Feature
{

    TimeoutFeature()
    {
        DependsOn<TimeoutManager>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        //context.Container.ConfigureComponent<TimeoutInstaller>(DependencyLifecycle.InstancePerCall);
        var connectionString = context.Settings.GetConnectionString();
        var schema = context.Settings.GetSchema();
        var endpointName = context.Settings.EndpointName();
        var timeoutPersister = new TimeoutPersister(connectionString, schema, endpointName);
        context.Container.ConfigureComponent(() => timeoutPersister, DependencyLifecycle.InstancePerCall);
    }
}