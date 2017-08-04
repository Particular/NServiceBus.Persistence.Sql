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
        var sqlVariant = settings.GetSqlDialect();
        var connectionBuilder = settings.GetConnectionBuilder();
        var tablePrefix = settings.GetTablePrefix();
        var schema= settings.GetSchema();
        var timeoutsCleanupExecutionInterval = context.Settings.GetOrDefault<TimeSpan?>("SqlPersistence.Timeout.CleanupExecutionInterval") ?? TimeSpan.FromMinutes(2);

        ConfigValidation.ValidateTableSettings(sqlVariant, tablePrefix, schema);

        var persister = new TimeoutPersister(connectionBuilder, tablePrefix, sqlVariant, schema, timeoutsCleanupExecutionInterval);
        context.Container.RegisterSingleton(typeof(IPersistTimeouts), persister);
        context.Container.RegisterSingleton(typeof(IQueryTimeouts), persister);
    }
}