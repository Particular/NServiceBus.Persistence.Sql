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
        var connectionManager = settings.GetConnectionBuilder<StorageType.Outbox>();
        var tablePrefix = settings.GetTablePrefix();
        var sqlDialect = settings.GetSqlDialect();

        var pessimisticMode = context.Settings.GetOrDefault<bool>(ConcurrencyMode);
        var transactionScopeMode = context.Settings.GetOrDefault<bool>(TransactionMode);

        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, tablePrefix);

        OutboxBehavior behavior;
        if (pessimisticMode)
        {
            behavior = new PessimisticOutboxBehavior(sqlDialect, outboxCommands);
        }
        else
        {
            behavior = new OptimisticOutboxBehavior(sqlDialect, outboxCommands);
        }

        ISqlOutboxTransaction transactionFactory()
        {
            return transactionScopeMode
                ? (ISqlOutboxTransaction)new TransactionScopeSqlOutboxTransaction(behavior, connectionManager)
                : new AdoNetSqlOutboxTransaction(behavior, connectionManager);
        }

        var outboxPersister = new OutboxPersister(connectionManager, sqlDialect, outboxCommands, transactionFactory);
        context.Container.ConfigureComponent(b => outboxPersister, DependencyLifecycle.InstancePerCall);

        if (settings.GetOrDefault<bool>(DisableCleanup))
        {
            settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Outbox", new
            {
                CleanupDisabled = true
            });

            return;
        }

        if (settings.EndpointIsMultiTenant())
        {
            throw new Exception($"{nameof(SqlPersistenceConfig.MultiTenantConnectionBuilder)} can only be used with the Outbox feature if Outbox cleanup is handled by an external process (i.e. SQL Agent) and the endpoint is configured to disable Outbox cleanup using endpointConfiguration.EnableOutbox().{nameof(SqlPersistenceOutboxSettingsExtensions.DisableCleanup)}(). See the SQL Persistence documentation for more information on how to clean up Outbox tables from a scheduled task.");
        }

        var frequencyToRunCleanup = settings.GetOrDefault<TimeSpan?>(FrequencyToRunDeduplicationDataCleanup) ?? TimeSpan.FromMinutes(1);
        var timeToKeepDeduplicationData = settings.GetOrDefault<TimeSpan?>(TimeToKeepDeduplicationData) ?? TimeSpan.FromDays(7);

        settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Outbox", new
        {
            CleanupDisabled = false,
            TimeToKeepDeduplicationData = timeToKeepDeduplicationData,
            FrequencyToRunDeduplicationDataCleanup = frequencyToRunCleanup
        });

        context.RegisterStartupTask(b =>
            new OutboxCleaner(outboxPersister.RemoveEntriesOlderThan, b.Build<CriticalError>().Raise, timeToKeepDeduplicationData, frequencyToRunCleanup, new AsyncTimer()));
    }

    internal const string TimeToKeepDeduplicationData = "Persistence.Sql.Outbox.TimeToKeepDeduplicationData";
    internal const string FrequencyToRunDeduplicationDataCleanup = "Persistence.Sql.Outbox.FrequencyToRunDeduplicationDataCleanup";
    internal const string DisableCleanup = "Persistence.Sql.Outbox.DisableCleanup";
    internal const string ConcurrencyMode = "Persistence.Sql.Outbox.PessimisticMode";
    internal const string TransactionMode = "Persistence.Sql.Outbox.TransactionScopeMode";
}