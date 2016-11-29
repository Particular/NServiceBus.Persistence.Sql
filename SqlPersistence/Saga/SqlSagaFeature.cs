using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

class SqlSagaFeature : Feature
{
    SqlSagaFeature()
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
        var jsonSerializer = settings.GetJsonSerializer<StorageType.Sagas>();
        var readerCreator = settings.GetReaderCreator<StorageType.Sagas>();
        var writerCreator = settings.GetWriterCreator<StorageType.Sagas>();
        var versionDeserializeBuilder = settings.GetVersionSettings();
        var infoCache = new SagaInfoCache(versionDeserializeBuilder, jsonSerializer, readerCreator, writerCreator, commandBuilder);
        var sagaPersister = new SagaPersister(infoCache);
        context.Container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
    }
}