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
        var endpointName = settings.EndpointName().ToString();

        var commandBuilder = new SagaCommandBuilder(schema, endpointName);
        var serialize = settings.GetSerializeBuilder();
        var deserialize = settings.GetDeserializeBuilder();
        var infoCache = new SagaInfoCache(deserialize, serialize, commandBuilder, settings.GetXmlSerializerCustomize());
        context.Container.ConfigureComponent(() => new SagaPersister(connectionString, infoCache), DependencyLifecycle.InstancePerCall);
    }
}