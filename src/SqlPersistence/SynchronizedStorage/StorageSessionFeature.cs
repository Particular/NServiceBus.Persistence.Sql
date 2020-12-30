using System;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence;
using NServiceBus.Sagas;
using NServiceBus.Settings;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using SagaSettings = NServiceBus.Persistence.Sql.SagaSettings;

class StorageSessionFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;
        ValidateSagaOutboxCombo(settings);

        var sqlDialect = settings.GetSqlDialect();
        var services = context.Services;
        var connectionManager = settings.GetConnectionBuilder<StorageType.Sagas>();

        var sqlSagaPersistenceActivated = settings.IsFeatureActive(typeof(SqlSagaFeature));

        SagaInfoCache infoCache = null;
        if (sqlSagaPersistenceActivated)
        {
            infoCache = BuildSagaInfoCache(sqlDialect, settings);
        }

        var sessionHolder = new CurrentSessionHolder();

        //Info cache can be null if Outbox is enabled but Sagas are disabled.
        services.AddSingleton<ISynchronizedStorage>(new SynchronizedStorage(connectionManager, infoCache, sessionHolder));
        services.AddSingleton<ISynchronizedStorageAdapter>(new StorageAdapter(connectionManager, infoCache, sqlDialect, sessionHolder));

        services.AddTransient(_ => sessionHolder.Current);
        context.Pipeline.Register(new CurrentSessionBehavior(sessionHolder), "Manages the lifecycle of the current session holder.");

        if (sqlSagaPersistenceActivated)
        {
            var sagaPersister = new SagaPersister(infoCache, sqlDialect);
            services.AddSingleton<ISagaPersister>(sagaPersister);
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
            name => nameFilter(name));
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