using System;
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
        var schema = settings.GetSchema();
        var sqlVariant = settings.GetSqlDialect();
        var outboxPersister = new OutboxPersister(connectionBuilder, tablePrefix, schema, sqlVariant);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
        context.RegisterStartupTask(b => new OutboxCleaner(outboxPersister.RemoveEntriesOlderThan, b.Build<CriticalError>().Raise, TimeSpan.FromDays(7), TimeSpan.FromMinutes(1), new AsyncTimer()));
    }
}