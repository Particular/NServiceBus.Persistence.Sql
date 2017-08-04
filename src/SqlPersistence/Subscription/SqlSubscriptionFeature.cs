using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
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

        var connectionBuilder = settings.GetConnectionBuilder();
        var tablePrefix = settings.GetTablePrefix();
        var sqlVariant = settings.GetSqlDialect();
        var schema = settings.GetSchema();
        var cacheFor = SubscriptionSettings.GetCacheFor(settings);
        var persister = new SubscriptionPersister(connectionBuilder, tablePrefix, sqlVariant, schema, cacheFor);

        ConfigValidation.ValidateTableSettings(sqlVariant, tablePrefix, schema);

        context.Container.RegisterSingleton(typeof (ISubscriptionStorage), persister);
    }
}