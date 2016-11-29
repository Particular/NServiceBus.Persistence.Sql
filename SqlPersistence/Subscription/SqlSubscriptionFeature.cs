using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SqlSubscriptionFeature : Feature
{
    SqlSubscriptionFeature()
    {
        DependsOn<MessageDrivenSubscriptions>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        settings.EnableFeature<StorageType.Subscriptions>();

        var connectionString = settings.GetConnectionString<StorageType.Subscriptions>();
        var schema = settings.GetSchema<StorageType.Subscriptions>();
        var endpointName = settings.GetEndpointNamePrefix<StorageType.Subscriptions>();
        var persister = new SubscriptionPersister(connectionString, schema, endpointName);
        context.Container.RegisterSingleton(typeof (ISubscriptionStorage), persister);
    }
}