using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Settings;

class SqlStorageSessionFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        ValidateSagaOutboxCombo(settings);

        var sqlDialect = settings.GetSqlDialect();
        var services = context.Services;
        var connectionManager = settings.GetConnectionBuilder<StorageType.Sagas>();

        var sessionHolder = new CurrentSessionHolder();

        //Info cache can be null if Outbox is enabled but Sagas are disabled.
        services.AddSingleton<ISynchronizedStorage>(provider => new SynchronizedStorage(connectionManager, provider.GetService<SagaInfoCache>(), sessionHolder));
        services.AddSingleton<ISynchronizedStorageAdapter>(provider => new StorageAdapter(connectionManager, provider.GetService<SagaInfoCache>(), sqlDialect, sessionHolder));

        services.AddTransient(_ => sessionHolder.Current);
        context.Pipeline.Register(new CurrentSessionBehavior(sessionHolder), "Manages the lifecycle of the current session holder.");
    }

    static void ValidateSagaOutboxCombo(IReadOnlySettings settings)
    {
        var isOutboxEnabled = settings.IsFeatureActive(typeof(Outbox));
        var isSagasEnabled = settings.IsFeatureActive(typeof(Sagas));
        if (!isOutboxEnabled || !isSagasEnabled)
        {
            return;
        }
        var isSagasEnabledForSqlPersistence = settings.IsFeatureActive(typeof(SqlSagaFeature));
        var isOutboxEnabledForSqlPersistence = settings.IsFeatureActive(typeof(SqlOutboxFeature));
        if (isSagasEnabledForSqlPersistence && isOutboxEnabledForSqlPersistence)
        {
            return;
        }
        throw new Exception("Sql Persistence must be enabled for either both Sagas and Outbox, or neither.");
    }
}