using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using NServiceBus.Settings;
using SagaSettings = NServiceBus.Persistence.Sql.SagaSettings;

class SqlSagaFeature : Feature
{
    SqlSagaFeature()
    {
        Defaults(s =>
        {
            s.AddUnrecoverableException(typeof(SerializationException));
        });
        DependsOn<Sagas>();
        Enable<SqlStorageSessionFeature>();
        DependsOn<SqlStorageSessionFeature>();
        Enable<ManifestOutput>();
        DependsOnOptionally<ManifestOutput>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;

        var sqlDialect = settings.GetSqlDialect();
        var services = context.Services;

        services.AddSingleton(BuildSagaInfoCache(sqlDialect, settings));
        services.AddSingleton<ISagaPersister>(provider => new SagaPersister(provider.GetRequiredService<SagaInfoCache>(), sqlDialect));

        var customJsonSettings = SagaSettings.GetJsonSerializerSettings(settings);
        var versionSpecificJsonSettings = SagaSettings.GetVersionSettings(settings);
        var customSagaWriter = SagaSettings.GetWriterCreator(settings);
        var customSagaReader = SagaSettings.GetReaderCreator(settings);

        settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Sagas", new
        {
            CustomJsonSettings = customJsonSettings != null,
            VersionSpecificJsonSettings = versionSpecificJsonSettings != null,
            CustomSagaWriter = customSagaWriter != null,
            CustomSagaReader = customSagaReader != null
        });

        if (settings.TryGet<ManifestOutput.PersistenceManifest>(out var manifest))
        {
            manifest.SetSagas(() => settings.Get<SagaMetadataCollection>().Select(
                        saga => GetSagaTableSchema(saga.Name, saga.EntityName, saga.TryGetCorrelationProperty(out var correlationProperty) ? correlationProperty.Name : null)).ToArray());

            ManifestOutput.PersistenceManifest.SagaManifest GetSagaTableSchema(string sagaName, string entityName, string correlationProperty) => new()
            {
                Name = sagaName,
                TableName = sqlDialect.GetSagaTableName($"{manifest.Prefix}_", entityName),
                Indexes = !string.IsNullOrEmpty(correlationProperty)
                            ? [new() { Name = $"Index_Correlation_{correlationProperty}", Columns = correlationProperty }]
                            : []
            };
        }
    }

    static SagaInfoCache BuildSagaInfoCache(SqlDialect sqlDialect, IReadOnlySettings settings)
    {
        var jsonSerializerSettings = SagaSettings.GetJsonSerializerSettings(settings);
        var jsonSerializer = BuildJsonSerializer(jsonSerializerSettings);
        sqlDialect.ValidateJsonSettings(jsonSerializer);
        var readerCreator = SagaSettings.GetReaderCreator(settings);
        readerCreator ??= reader => new JsonTextReader(reader);
        var writerCreator = SagaSettings.GetWriterCreator(settings);
        writerCreator ??= writer => new JsonTextWriter(writer);
        var nameFilter = SagaSettings.GetNameFilter(settings);
        nameFilter ??= sagaName => sagaName;
        var versionDeserializeBuilder = SagaSettings.GetVersionSettings(settings);
        var tablePrefix = settings.GetTablePrefix(settings.EndpointName());
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