using System;
using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;
using NServiceBus.Settings;

class StorageSessionFeature : Feature
{

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        ValidateSagaOutboxCombo(settings);
        var connectionBuilder = settings.GetConnectionBuilder();
        var container = context.Container;
        container.ConfigureComponent(builder => new SynchronizedStorage(connectionBuilder, GetInfoCache(builder)), DependencyLifecycle.SingleInstance);
        container.ConfigureComponent(builder => new StorageAdapter(connectionBuilder, GetInfoCache(builder)), DependencyLifecycle.SingleInstance);
    }

    static void ValidateSagaOutboxCombo(ReadOnlySettings settings)
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
        throw new Exception("Sql Persistence must be enable for either both Sagas and Outbox, or neither.");
    }

    static SagaInfoCache GetInfoCache(IBuilder builder)
    {
        return builder.BuildAll<SagaInfoCache>().SingleOrDefault();
    }
}