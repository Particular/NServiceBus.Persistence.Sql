using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SqlXmlSubscriptionFeature : Feature
{
    SqlXmlSubscriptionFeature()
    {
        DependsOn<MessageDrivenSubscriptions>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Subscriptions>();

        var connectionString = context.Settings.GetConnectionString<StorageType.Subscriptions>();
        var schema = context.Settings.GetSchema<StorageType.Subscriptions>();
        var endpointName = context.Settings.ShouldUseEndpointName<StorageType.Subscriptions>()
            ? context.Settings.EndpointName() + "."
            : "";

        var persister = new SubscriptionPersister(connectionString, schema, endpointName);
        context.Container.RegisterSingleton(typeof (ISubscriptionStorage), persister);
    }
}