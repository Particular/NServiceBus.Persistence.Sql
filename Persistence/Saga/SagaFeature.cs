using NServiceBus;
using NServiceBus.Features;

class SagaFeature : Feature
{
    SagaFeature()
    {
        DependsOn<Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionString = context.Settings.GetConnectionString();
        var schema = context.Settings.GetSchema();
        var endpointName = context.Settings.EndpointName();
        var persister = new SagaPersister(connectionString, schema, endpointName);
        context.Container.ConfigureComponent(() => persister, DependencyLifecycle.InstancePerCall);
    }
}