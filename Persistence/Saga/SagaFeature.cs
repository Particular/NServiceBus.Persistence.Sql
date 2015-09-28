using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;

class SagaFeature : Feature
{
    SagaFeature()
    {
        DependsOn<Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var connectionString = context.Settings.GetConnectionString<StorageType.Sagas>();
        var schema = context.Settings.GetSchema<StorageType.Sagas>();
        var endpointName = context.Settings.EndpointName();

        var commandBuilder = new SagaCommandBuilder(schema,endpointName);
        var sagaInfoCache = new SagaInfoCache(null,null, commandBuilder);
        context.Container.ConfigureComponent(() => new SagaPersister(connectionString,sagaInfoCache), DependencyLifecycle.InstancePerCall);
    }
}