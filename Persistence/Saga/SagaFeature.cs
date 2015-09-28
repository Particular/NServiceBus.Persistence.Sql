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
        var settings = context.Settings;
        var connectionString = settings.GetConnectionString<StorageType.Sagas>();
        var schema = settings.GetSchema<StorageType.Sagas>();
        var endpointName = settings.EndpointName();

        var commandBuilder = new SagaCommandBuilder(schema,endpointName);
        var serializeBuilder = settings.GetSerializeBuilder();
        var deserializeBuilder = settings.GetDeserializeBuilder();
        var sagaInfoCache = new SagaInfoCache(deserializeBuilder, serializeBuilder, commandBuilder);
        context.Container.ConfigureComponent(() => new SagaPersister(connectionString,sagaInfoCache), DependencyLifecycle.InstancePerCall);
    }
}