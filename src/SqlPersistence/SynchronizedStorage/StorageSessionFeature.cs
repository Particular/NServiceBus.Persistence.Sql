using System;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using NServiceBus.Settings;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

class StorageSessionFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        ValidateSagaOutboxCombo(settings);

        var sqlDialect = settings.GetSqlDialect();
        var container = context.Container;
        var connectionBuilder = settings.GetConnectionBuilder();

        var sqlSagaPersistenceActivated = settings.IsFeatureActive(typeof(SqlSagaFeature));

        SagaInfoCache infoCache = null;
        if (sqlSagaPersistenceActivated)
        {
            infoCache = BuildSagaInfoCache(sqlDialect, settings);
        }

        //Info cache can be null if Outbox is enabled but Sagas are disabled.
        container.ConfigureComponent(() => new SynchronizedStorage(connectionBuilder, infoCache), DependencyLifecycle.SingleInstance);
        container.ConfigureComponent(() => new StorageAdapter(connectionBuilder, infoCache, sqlDialect), DependencyLifecycle.SingleInstance);

        if (sqlSagaPersistenceActivated)
        {
            var sagaPersister = new SagaPersister(infoCache, sqlDialect);
            container.ConfigureComponent<ISagaPersister>(() => sagaPersister, DependencyLifecycle.SingleInstance);
        }
    }

    static void ValidateSagaOutboxCombo(ReadOnlySettings settings)
    {
        var isOutboxEnabled = settings.IsFeatureActive(typeof(Outbox));
        var isSagasEnabled = settings.IsFeatureActive(typeof(Sagas));
        if (!isOutboxEnabled || !isSagasEnabled)
        {
            return;
        }
        var isSagasEnabledForSqlPersistence = settings.IsFeatureActive(typeof(SqlSagaFeature));
        var isOutboxEnabledForSqlPersistence = settings.IsFeatureActive(typeof(SqlOutboxFeature));
        if (isSagasEnabledForSqlPersistence && isOutboxEnabledForSqlPersistence)
        {
            return;
        }
        throw new Exception("Sql Persistence must be enabled for either both Sagas and Outbox, or neither.");
    }

    static SagaInfoCache BuildSagaInfoCache(SqlDialect sqlDialect, ReadOnlySettings settings)
    {
        var jsonSerializerSettings = SagaSettings.GetJsonSerializerSettings(settings);
        var jsonSerializer = BuildJsonSerializer(jsonSerializerSettings);
        sqlDialect.ValidateJsonSettings(jsonSerializer);
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
        var nameFilter = SagaSettings.GetNameFilter(settings);
        if (nameFilter == null)
        {
            nameFilter = sagaName => sagaName;
        }
        var versionDeserializeBuilder = SagaSettings.GetVersionSettings(settings);
        var tablePrefix = settings.GetTablePrefix();
        return new SagaInfoCache(
            versionSpecificSettings: versionDeserializeBuilder,
            jsonSerializer: jsonSerializer,
            readerCreator: readerCreator,
            writerCreator: writerCreator,
            tablePrefix: tablePrefix,
            sqlDialect: sqlDialect,
            metadataCollection: settings.Get<SagaMetadataCollection>(),
            nameFilter: nameFilter);
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