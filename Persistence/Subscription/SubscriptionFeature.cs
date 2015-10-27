using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SubscriptionFeature : Feature
{
    SubscriptionFeature()
    {
        DependsOn<MessageDrivenSubscriptions>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionString = context.Settings.GetConnectionString<StorageType.Subscriptions>();
        var schema = context.Settings.GetSchema<StorageType.Subscriptions>();
        var endpointName = context.Settings.EndpointName().ToString();
        var persister = new SubscriptionPersister(connectionString, schema, endpointName);
        context.Container.RegisterSingleton(typeof (ISubscriptionStorage), persister);
    }
}