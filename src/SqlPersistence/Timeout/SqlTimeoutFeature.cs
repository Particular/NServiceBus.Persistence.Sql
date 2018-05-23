using System;
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
        var TimeoutsCleanupExecutionInterval = context.Settings.GetOrDefault<TimeSpan?>("SqlPersistence.Timeout.CleanupExecutionInterval") ?? TimeSpan.FromMinutes(2);

        sqlDialect.ValidateTablePrefix(tablePrefix);

        settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Timeouts", new
        {
            TimeoutsCleanupExecutionInterval
        });

        var persister = new TimeoutPersister(connectionBuilder, tablePrefix, sqlDialect, TimeoutsCleanupExecutionInterval, () => DateTime.UtcNow);
        context.Container.RegisterSingleton(typeof(IPersistTimeouts), persister);
        context.Container.RegisterSingleton(typeof(IQueryTimeouts), persister);
    }
}