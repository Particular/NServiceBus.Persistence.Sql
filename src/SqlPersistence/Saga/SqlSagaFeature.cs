using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.Sql;

class SqlSagaFeature : Feature
{
    SqlSagaFeature()
    {
        DependsOn<Sagas>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var settings = context.Settings;

        var customJsonSettings = SagaSettings.GetJsonSerializerSettings(settings);
        var versionSpecificJsonSettings = SagaSettings.GetVersionSettings(settings);
        var customSagaWriter = SagaSettings.GetWriterCreator(settings);
        var customSagaReader = SagaSettings.GetReaderCreator(settings);

        settings.AddStartupDiagnosticsSection("NServiceBus.Persistence.Sql.Sagas", new Dictionary<string, object>
        {
            { nameof(customJsonSettings), customJsonSettings != null},
            { nameof(versionSpecificJsonSettings), versionSpecificJsonSettings != null},
            { nameof(customSagaWriter), customSagaWriter != null},
            { nameof(customSagaReader), customSagaReader != null}
        });
    }
}