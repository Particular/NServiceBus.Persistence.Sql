using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;

class SqlSagaFeature : Feature
{
    SqlSagaFeature()
    {
        Defaults(s =>
        {
            s.EnableFeatureByDefault<SqlStorageSessionFeature>();
            s.AddUnrecoverableException(typeof(SerializationException));
        });
        DependsOn<Sagas>();
        DependsOn<SqlStorageSessionFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;

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
    }
}