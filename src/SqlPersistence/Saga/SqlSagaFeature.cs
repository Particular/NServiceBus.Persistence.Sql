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
        var settings = context.Settings;
        settings.EnableFeature<StorageType.Sagas>();

        var sqlVariant = settings.GetSqlVariant();
#pragma warning disable 618
        var commandBuilder = new SagaCommandBuilder(sqlVariant);
#pragma warning restore 618
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
        var tablePrefix = settings.GetTablePrefix();
        var schema = settings.GetSchema();
        var infoCache = new SagaInfoCache(
            versionSpecificSettings: versionDeserializeBuilder,
            jsonSerializer: jsonSerializer,
            readerCreator: readerCreator,
            writerCreator: writerCreator,
            commandBuilder: commandBuilder,
            tablePrefix: tablePrefix,
            schema: schema,
            sqlVariant: sqlVariant,
            metadataCollection: settings.Get<SagaMetadataCollection>());
        var sagaPersister = new SagaPersister(infoCache, sqlVariant);
        var container = context.Container;
        container.ConfigureComponent(() => infoCache, DependencyLifecycle.SingleInstance);
        container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
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