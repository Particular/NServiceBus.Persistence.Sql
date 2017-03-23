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
        var tablePrefix = settings.GetTablePrefix();
        var sqlVariant = settings.GetSqlVariant();
        var schema = settings.GetSchema();

        ConfigValidation.ValidateTableSettings(sqlVariant, tablePrefix, schema);

        var persister = new SubscriptionPersister(connectionBuilder, tablePrefix, sqlVariant, schema);
        context.Container.RegisterSingleton(typeof (ISubscriptionStorage), persister);
    }
}