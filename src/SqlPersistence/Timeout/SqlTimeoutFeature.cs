using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Timeout.Core;

class SqlTimeoutFeature : Feature
{
    SqlTimeoutFeature()
    {
        DependsOn<TimeoutManager>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        var sqlDialect = settings.GetSqlDialect();
        var connectionManager = settings.GetConnectionBuilder<StorageType.Timeouts>();
        var tablePrefix = settings.GetTablePrefix();
        var timeoutsCleanupExecutionInterval = context.Settings.GetOrDefault<TimeSpan?>("SqlPersistence.Timeout.CleanupExecutionInterval") ?? TimeSpan.FromMinutes(2);

        sqlDialect.ValidateTablePrefix(tablePrefix);

        settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Timeouts", new
        {
            TimeoutsCleanupExecutionInterval = timeoutsCleanupExecutionInterval,
            CustomConnectionBuilder = settings.HasSetting($"SqlPersistence.ConnectionManager.{typeof(StorageType.Timeouts).Name}")
        });

        var persister = new TimeoutPersister(connectionManager, tablePrefix, sqlDialect, timeoutsCleanupExecutionInterval, () => DateTime.UtcNow);
        context.Services.AddSingleton(typeof(IPersistTimeouts), persister);
        context.Services.AddSingleton(typeof(IQueryTimeouts), persister);
    }
}