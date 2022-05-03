using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SqlStorageSessionFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        var sqlDialect = settings.GetSqlDialect();
        var services = context.Services;
        var connectionManager = settings.GetConnectionBuilder<StorageType.Sagas>();

        //Info cache can be null if Outbox is enabled but Sagas are disabled.
        // TODO: The connection manager design needs a revisit to avoid the closure
        services.AddScoped<ICompletableSynchronizedStorageSession>(provider => new StorageSession(connectionManager, provider.GetService<SagaInfoCache>(), sqlDialect));
        services.AddScoped(provider => provider.GetService<ISynchronizedStorageSession>().SqlPersistenceSession());
    }
}