using System;
using System.Collections.Generic;
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
        var connectionBuilder = settings.GetConnectionBuilder<StorageType.Timeouts>();
        var tablePrefix = settings.GetTablePrefix();
        var timeoutsCleanupExecutionInterval = context.Settings.GetOrDefault<TimeSpan?>("SqlPersistence.Timeout.CleanupExecutionInterval") ?? TimeSpan.FromMinutes(2);

        sqlDialect.ValidateTablePrefix(tablePrefix);

        var diagnostics = new Dictionary<string, object>
        {
            { nameof(timeoutsCleanupExecutionInterval), timeoutsCleanupExecutionInterval }
        };
        sqlDialect.AddExtraDiagnosticsInfo(diagnostics);

        settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Timeouts", new
        {
            diagnostics
        });

        var persister = new TimeoutPersister(connectionBuilder, tablePrefix, sqlDialect, timeoutsCleanupExecutionInterval, () => DateTime.UtcNow);
        context.Container.RegisterSingleton(typeof(IPersistTimeouts), persister);
        context.Container.RegisterSingleton(typeof(IQueryTimeouts), persister);
    }
}