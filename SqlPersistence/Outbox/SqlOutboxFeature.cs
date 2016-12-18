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
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Outbox>();
        var sqlVarient = settings.GetSqlVarient();
        var endpointName = settings.GetTablePrefix<StorageType.Outbox>();
        var outboxPersister = new OutboxPersister(sqlVarient, connectionBuilder, endpointName);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
    }
}