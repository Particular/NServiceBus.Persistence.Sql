using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using NServiceBus.Settings;
using SagaSettings = NServiceBus.Persistence.Sql.SagaSettings;

sealed class SqlSagaFeature : Feature
{
    public SqlSagaFeature()
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

        // Resolved once and shared by the persister and the manifest so the two cannot report
        // different tables for the same saga.
        var tablePrefix = settings.GetTablePrefix(settings.EndpointName());
        Func<string, string> nameFilter = SagaSettings.GetNameFilter(settings) ?? (sagaName => sagaName);

        services.AddSingleton(BuildSagaInfoCache(sqlDialect, settings, tablePrefix, nameFilter));
        services.AddSingleton<ISagaPersister>(provider => new SagaPersister(provider.GetRequiredService<SagaInfoCache>(), sqlDialect));

        var installerSettings = settings.GetOrDefault<InstallerSettings>();

        if (!installerSettings.Disabled && !settings.EndpointIsMultiTenant())
        {
            context.AddInstaller<SagaInstaller>();
        }

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
            manifest.SetSagas(() => BuildSagaManifests(
                settings.Get<SagaMetadataCollection>(),
                sqlDialect,
                tablePrefix,
                nameFilter));
        }
    }

    internal static ManifestOutput.PersistenceManifest.SagaManifest[] BuildSagaManifests(
        SagaMetadataCollection metadataCollection,
        SqlDialect sqlDialect,
        string tablePrefix,
        Func<string, string> nameFilter) =>
        metadataCollection.Select(metadata =>
        {
            // Resolve suffix and correlation property the same way the runtime does, so the manifest
            // can't drift from it, including honouring an explicit [SqlSaga(correlationProperty: ...)].
            var typeData = SqlSagaTypeDataReader.GetTypeData(metadata);
            return new ManifestOutput.PersistenceManifest.SagaManifest
            {
                Name = metadata.Name,
                TableName = sqlDialect.GetSagaTableName(tablePrefix, nameFilter(typeData.TableSuffix)),
                Indexes = !string.IsNullOrEmpty(typeData.CorrelationProperty)
                    ?
                    [
                        new()
                        {
                            Name = $"Index_Correlation_{typeData.CorrelationProperty}",
                            Columns = typeData.CorrelationProperty
                        }
                    ]
                    : []
            };
        }).ToArray();

    static SagaInfoCache BuildSagaInfoCache(
        SqlDialect sqlDialect,
        IReadOnlySettings settings,
        string tablePrefix,
        Func<string, string> nameFilter)
    {
        var jsonSerializerSettings = SagaSettings.GetJsonSerializerSettings(settings);
        var jsonSerializer = BuildJsonSerializer(jsonSerializerSettings);
        sqlDialect.ValidateJsonSettings(jsonSerializer);
        var readerCreator = SagaSettings.GetReaderCreator(settings);
        readerCreator ??= reader => new JsonTextReader(reader);
        var writerCreator = SagaSettings.GetWriterCreator(settings);
        writerCreator ??= writer => new JsonTextWriter(writer);
        var versionDeserializeBuilder = SagaSettings.GetVersionSettings(settings);
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