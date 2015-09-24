using NServiceBus;
using NServiceBus.Features;

class SubscriptionFeature : Feature
{
    SubscriptionFeature()
    {
        DependsOn<StorageDrivenPublishing>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionString = context.Settings.GetConnectionString();
        var schema = context.Settings.GetSchema();
        var endpointName = context.Settings.EndpointName();
        var persister = new SubscriptionPersister(connectionString, schema, endpointName);
        context.Container.ConfigureComponent(() => persister, DependencyLifecycle.InstancePerCall);
    }
}