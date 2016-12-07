using System.IO;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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

        var endpointName = settings.GetTablePrefixForEndpoint<StorageType.Sagas>();
        var commandBuilder = new SagaCommandBuilder(schema, endpointName);
        var jsonSerializerSettings = SagaSettings.GetJsonSerializerSettings(settings);
        JsonSerializer jsonSerializer;
        if (jsonSerializerSettings == null)
        {
            jsonSerializer = new JsonSerializer();
        }
        else
        {
            jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);
        }
        var readerCreator = SagaSettings.GetReaderCreator(settings);
        if (readerCreator == null)
        {
            readerCreator = reader => new JsonTextReader(reader);
        }
        var writerCreator = SagaSettings.GetWriterCreator(settings);
        if (writerCreator == null)
        {
            writerCreator = builder =>
            {
                var writer = new StringWriter(builder);
                return new JsonTextWriter(writer);
            };
        }
        var versionDeserializeBuilder = SagaSettings.GetVersionSettings(settings);
        var infoCache = new SagaInfoCache(versionDeserializeBuilder, jsonSerializer, readerCreator, writerCreator, commandBuilder);
        var sagaPersister = new SagaPersister(infoCache);
        context.Container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
    }
}