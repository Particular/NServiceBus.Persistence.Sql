using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
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

        var endpointName = settings.GetEndpointNamePrefix<StorageType.Sagas>();
        var commandBuilder = new SagaCommandBuilder(schema, endpointName);
        var serialize = settings.GetSerializeBuilder();
        if (serialize == null)
        {
            serialize = SagaXmlSerializerBuilder.BuildSerializationDelegate;
        }
        var versionDeserializeBuilder = settings.GetVersionDeserializeBuilder();
        var infoCache = new SagaInfoCache(versionDeserializeBuilder, serialize, commandBuilder);
        var sagaPersister = new SagaPersister(infoCache);
        context.Container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
    }
}