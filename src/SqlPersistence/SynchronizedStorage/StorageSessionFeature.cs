using System;
using System.Linq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;
using NServiceBus.Persistence;
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
        var isSagasEnabledForSqlPersistence = settings.GetFeatureEnabled<StorageType.Sagas>();
        var isOutboxEnabledForSqlPersistence = settings.GetFeatureEnabled<StorageType.Outbox>();
        var isOutboxEnabled = settings.IsFeatureActive(typeof(Outbox)) || settings.IsFeatureEnabled(typeof(Outbox));
        var isSagasEnabled = settings.IsFeatureActive(typeof(Sagas)) || settings.IsFeatureEnabled(typeof(Sagas));
        if (isOutboxEnabled && isSagasEnabled)
        {
            if (!isSagasEnabledForSqlPersistence || !isOutboxEnabledForSqlPersistence)
            {
                throw new Exception("Sql Persistence must be enable for either both Sagas and Outbox, or neither.");
            }
        }
    }

    static SagaInfoCache GetInfoCache(IBuilder builder)
    {
        return builder.BuildAll<SagaInfoCache>().SingleOrDefault();
    }
}