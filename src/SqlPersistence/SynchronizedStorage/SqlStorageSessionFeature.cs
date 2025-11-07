using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Settings;

sealed class SqlStorageSessionFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        // the settings are deliberately acquired here to make sure exceptions are raised in case of misconfiguration
        // during feature setup time. Later the same settings are resolved from DI to avoid allocation closures
        // everytime the scope synchronized storage session is retrieved.
        _ = context.Settings.GetSqlDialect();
        _ = context.Settings.GetConnectionBuilder<StorageType.Sagas>();

        var services = context.Services;
        services.AddScoped<ICompletableSynchronizedStorageSession>(provider =>
        {
            var settings = provider.GetRequiredService<IReadOnlySettings>();
            var sqlDialect = settings.GetSqlDialect();
            var connectionManager = settings.GetConnectionBuilder<StorageType.Sagas>();
            //Info cache can be null if Outbox is enabled but Sagas are disabled.
            var sagaInfoCache = provider.GetService<SagaInfoCache>();

            return new StorageSession(connectionManager, sagaInfoCache, sqlDialect);
        });
        services.AddScoped(sp => (sp.GetService<ICompletableSynchronizedStorageSession>() as ISqlStorageSession)!);
    }
}
