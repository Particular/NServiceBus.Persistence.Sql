using System;
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
        var sqlVariant = settings.GetSqlVariant();
        var endpointName = settings.GetTablePrefix();
        var outboxPersister = new OutboxPersister(sqlVariant, connectionBuilder, endpointName);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);
        var cleanerTask = new OutboxCleaner(outboxPersister, TimeSpan.FromDays(7), TimeSpan.FromMinutes(1)); //Default values from NHibernate persister
        context.RegisterStartupTask(cleanerTask);
    }
}