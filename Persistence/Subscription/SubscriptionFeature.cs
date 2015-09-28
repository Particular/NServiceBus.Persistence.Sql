using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

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
        context.Container.RegisterSingleton(typeof(ISubscriptionStorage), persister);
    }
}