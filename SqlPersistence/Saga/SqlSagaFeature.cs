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
        var endpointName = settings.GetTablePrefix<StorageType.Sagas>();
        var sqlVariant = settings.GetSqlVariant();
        var commandBuilder = new SagaCommandBuilder(sqlVariant,endpointName);
        var jsonSerializerSettings = SagaSettings.GetJsonSerializerSettings(settings);
        var jsonSerializer = BuildJsonSerializer(jsonSerializerSettings);
        var readerCreator = SagaSettings.GetReaderCreator(settings);
        if (readerCreator == null)
        {
            readerCreator = reader => new JsonTextReader(reader);
        }
        var writerCreator = SagaSettings.GetWriterCreator(settings);
        if (writerCreator == null)
        {
            writerCreator = writer => new JsonTextWriter(writer);
        }
        var versionDeserializeBuilder = SagaSettings.GetVersionSettings(settings);
        var infoCache = new SagaInfoCache(versionDeserializeBuilder, jsonSerializer, readerCreator, writerCreator, commandBuilder);
        var sagaPersister = new SagaPersister(infoCache);
        context.Container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
    }

     static JsonSerializer BuildJsonSerializer(JsonSerializerSettings jsonSerializerSettings)
    {
        if (jsonSerializerSettings == null)
        {
            return Serializer.JsonSerializer;
        }
        return JsonSerializer.Create(jsonSerializerSettings);
    }

}