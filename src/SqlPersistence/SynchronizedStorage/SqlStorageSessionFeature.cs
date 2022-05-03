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

        var sessionHolder = new CurrentSessionHolder();

        //Info cache can be null if Outbox is enabled but Sagas are disabled.
        services.AddSingleton<ISynchronizedStorage>(provider => new SynchronizedStorage(connectionManager, provider.GetService<SagaInfoCache>(), sessionHolder));
        services.AddSingleton<ISynchronizedStorageAdapter>(provider => new StorageAdapter(connectionManager, provider.GetService<SagaInfoCache>(), sqlDialect, sessionHolder));

        services.AddTransient(_ => sessionHolder.Current);
        context.Pipeline.Register(new CurrentSessionBehavior(sessionHolder), "Manages the lifecycle of the current session holder.");
    }
}