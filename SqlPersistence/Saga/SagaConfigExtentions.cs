using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;

public static class SagaConfigExtentions
{

    public static void JsonSettingsForVersion(this PersistenceExtensions<SqlPersistence, StorageType.Sagas> configuration, RetrieveVersionSpecificJsonSettings builder)
    {
        configuration.GetSettings()
            .Set<RetrieveVersionSpecificJsonSettings>(builder);
    }

    internal static RetrieveVersionSpecificJsonSettings GetVersionSettings(this ReadOnlySettings settings)
    {
        return settings.GetOrDefault<RetrieveVersionSpecificJsonSettings>();
    }

}