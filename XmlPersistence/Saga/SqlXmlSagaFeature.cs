using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

class SqlXmlSagaFeature : Feature
{
    SqlXmlSagaFeature()
    {
        DependsOn<Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.Settings.EnableFeature<StorageType.Sagas>();

        var settings = context.Settings;
        var schema = settings.GetSchema<StorageType.Sagas>();
        var endpointName = settings.ShouldUseEndpointName<StorageType.Sagas>()
            ? settings.EndpointName() + "."
            : "";

        var commandBuilder = new SagaCommandBuilder(schema, endpointName);
        var serialize = settings.GetSerializeBuilder();
        var deserialize = settings.GetDeserializeBuilder();
        var infoCache = new SagaInfoCache(deserialize, serialize, commandBuilder, settings.GetXmlSerializerCustomize());
        var sagaPersister = new SagaPersister(infoCache);
        context.Container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
    }
}