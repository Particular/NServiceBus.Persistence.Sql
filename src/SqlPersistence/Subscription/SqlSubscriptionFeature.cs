﻿using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

class SqlSubscriptionFeature : Feature
{
    SqlSubscriptionFeature()
    {
        DependsOn("NServiceBus.Features.MessageDrivenSubscriptions");
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;

        var connectionManager = settings.GetConnectionBuilder<StorageType.Subscriptions>();
        var tablePrefix = settings.GetTablePrefix();
        var sqlDialect = settings.GetSqlDialect();
        var cacheFor = SubscriptionSettings.GetCacheFor(settings);
        var persister = new SubscriptionPersister(connectionManager, tablePrefix, sqlDialect, cacheFor);

        sqlDialect.ValidateTablePrefix(tablePrefix);

        settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Subscriptions",
            new
            {
                EntriesCashedFor = cacheFor,
                CustomConnectionBuilder = settings.HasSetting($"SqlPersistence.ConnectionManager.{nameof(StorageType.Subscriptions)}")
            });

        context.Services.AddSingleton(typeof(ISubscriptionStorage), persister);
    }
}