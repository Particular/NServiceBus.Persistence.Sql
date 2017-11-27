using NServiceBus;
using NServiceBus.Features;

class SqlOutboxFeature : Feature
{
    SqlOutboxFeature()
    {
        DependsOn<Outbox>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        var connectionBuilder = settings.GetConnectionBuilder();
        var tablePrefix = settings.GetTablePrefix();
        var sqlDialect = settings.GetSqlDialect();
        var outboxPersister = new OutboxPersister(connectionBuilder, tablePrefix, sqlDialect);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
        context.RegisterStartupTask(b => new OutboxCleaner(outboxPersister.RemoveEntriesOlderThan, b.Build<CriticalError>().Raise,
            settings.GetTimeToKeepDeduplicationData(),
            settings.GetDeduplicationDataCleanupInterval(),
            settings.GetDeduplicationDataCleanupBatchSize(), 
            new AsyncTimer()));
    }
}