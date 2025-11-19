using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Outbox;

sealed class SqlOutboxFeature : Feature
{
    public SqlOutboxFeature()
    {
        DependsOn<Outbox>();
        Enable<SqlStorageSessionFeature>();
        DependsOn<SqlStorageSessionFeature>();
        Enable<ManifestOutput>();
        DependsOnOptionally<ManifestOutput>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        var connectionManager = settings.GetConnectionBuilder<StorageType.Outbox>();
        var endpointName = settings.GetOrDefault<string>(ProcessorEndpointKey) ?? settings.EndpointName();
        var tablePrefix = settings.GetTablePrefix(endpointName);
        var sqlDialect = settings.GetSqlDialect();

        var pessimisticMode = context.Settings.GetOrDefault<bool>(ConcurrencyMode);
        var transactionScopeMode = context.Settings.GetOrDefault<bool>(UseTransactionScope);

        var adoTransactionIsolationLevel = context.Settings.GetOrDefault<System.Data.IsolationLevel>(AdoTransactionIsolationLevel);
        if (adoTransactionIsolationLevel == default)
        {
            //Default to Read Committed
            adoTransactionIsolationLevel = System.Data.IsolationLevel.ReadCommitted;
        }

        var transactionScopeIsolationLevel = context.Settings.GetOrDefault<System.Transactions.IsolationLevel>(TransactionScopeIsolationLevel);
        var transactionScopeTimeout = context.Settings.GetOrDefault<TimeSpan>(TransactionScopeTimeout);

        var outboxCommands = OutboxCommandBuilder.Build(sqlDialect, tablePrefix);

        ConcurrencyControlStrategy concurrencyControlStrategy = pessimisticMode
            ? new PessimisticConcurrencyControlStrategy(sqlDialect, outboxCommands)
            : new OptimisticConcurrencyControlStrategy(sqlDialect, outboxCommands);

        ISqlOutboxTransaction transactionFactory() => transactionScopeMode
            ? new TransactionScopeSqlOutboxTransaction(concurrencyControlStrategy, connectionManager, transactionScopeIsolationLevel, transactionScopeTimeout)
            : new AdoNetSqlOutboxTransaction(concurrencyControlStrategy, connectionManager, adoTransactionIsolationLevel);

        var outboxPersister = new OutboxPersister(connectionManager, sqlDialect, outboxCommands, transactionFactory);
        _ = context.Services.AddTransient<IOutboxStorage>(_ => outboxPersister);

        var installerSettings = context.Settings.GetOrDefault<InstallerSettings>();
        if (!installerSettings.Disabled &&
            !installerSettings.DoNotCreateOutboxTable &&
            !settings.EndpointIsMultiTenant())
        {
            context.AddInstaller<OutboxInstaller>();
        }

        if (settings.TryGet<ManifestOutput.PersistenceManifest>(out var manifest))
        {
            manifest.SetOutbox(() => new ManifestOutput.PersistenceManifest.OutboxManifest
            {
                TableName = sqlDialect.GetOutboxTableName($"{manifest.Prefix}_")
            });
        }

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
            new OutboxCleaner(outboxPersister.RemoveEntriesOlderThan, b.GetRequiredService<CriticalError>().Raise, timeToKeepDeduplicationData, frequencyToRunCleanup, new AsyncTimer()));
    }

    internal const string TimeToKeepDeduplicationData = "Persistence.Sql.Outbox.TimeToKeepDeduplicationData";
    internal const string FrequencyToRunDeduplicationDataCleanup = "Persistence.Sql.Outbox.FrequencyToRunDeduplicationDataCleanup";
    internal const string DisableCleanup = "Persistence.Sql.Outbox.DisableCleanup";
    internal const string ConcurrencyMode = "Persistence.Sql.Outbox.PessimisticMode";
    internal const string UseTransactionScope = "Persistence.Sql.Outbox.TransactionScopeMode";
    internal const string AdoTransactionIsolationLevel = "Persistence.Sql.Outbox.AdoTransactionIsolationLevel";
    internal const string TransactionScopeIsolationLevel = "Persistence.Sql.Outbox.TransactionScopeIsolationLevel";
    internal const string TransactionScopeTimeout = "Persistence.Sql.Outbox.TransactionScopeTimeout";
    internal const string ProcessorEndpointKey = "Persistence.Sql.TransactionalSession.ProcessorEndpoint";
}