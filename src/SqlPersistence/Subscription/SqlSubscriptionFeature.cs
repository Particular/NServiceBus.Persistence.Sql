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

        var connectionBuilder = settings.GetConnectionBuilder();
        var endpointName = settings.GetTablePrefix();
        var sqlVariant = settings.GetSqlVariant();
        var persister = new SubscriptionPersister(connectionBuilder, endpointName, sqlVariant);
        context.Container.RegisterSingleton(typeof (ISubscriptionStorage), persister);
    }
}