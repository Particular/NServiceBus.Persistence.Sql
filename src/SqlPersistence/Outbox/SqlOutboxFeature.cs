using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlOutboxFeature : Feature
{
    SqlOutboxFeature()
    {
        DependsOn<Outbox>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Outbox>();
        var settings = context.Settings;
        var connectionBuilder = settings.GetConnectionBuilder();
        var tablePrefix = settings.GetTablePrefix();
        var schema = settings.GetSchema();
        var sqlVariant = settings.GetSqlVariant();
        var outboxPersister = new OutboxPersister(connectionBuilder, tablePrefix, schema, sqlVariant);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
    }
}