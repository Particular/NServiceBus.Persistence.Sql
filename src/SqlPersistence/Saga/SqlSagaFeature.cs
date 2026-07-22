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

        services.AddSingleton(BuildSagaInfoCache(sqlDialect, settings));
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
            var nameFilter = GetNameFilter(settings);
            manifest.SetSagas(() => settings.Get<SagaMetadataCollection>().Select(saga => CreateSagaManifest(saga, sqlDialect, $"{manifest.Prefix}_", nameFilter)).ToArray());
        }
    }

    internal static ManifestOutput.PersistenceManifest.SagaManifest CreateSagaManifest(SagaMetadata sagaMetadata, SqlDialect sqlDialect, string tablePrefix, Func<string, string> nameFilter)
    {
        var sqlSagaData = SqlSagaTypeDataReader.GetTypeData(sagaMetadata);
        var tableSuffix = nameFilter(sqlSagaData.TableSuffix);

        return new()
        {
            Name = sagaMetadata.Name,
            TableName = sqlDialect.GetSagaTableName(tablePrefix, tableSuffix),
            Indexes = !string.IsNullOrEmpty(sqlSagaData.CorrelationProperty)
                ?
                [
                    new()
                    {
                        Name = $"Index_Correlation_{sqlSagaData.CorrelationProperty}",
                        Columns = sqlSagaData.CorrelationProperty
                    }
                ]
                : []
        };
    }

    static Func<string, string> GetNameFilter(IReadOnlySettings settings)
    {
        return SagaSettings.GetNameFilter(settings) ?? (sagaName => sagaName);
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
        var nameFilter = GetNameFilter(settings);
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