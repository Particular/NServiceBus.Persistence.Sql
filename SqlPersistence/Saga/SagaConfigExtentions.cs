using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;

public static class SagaConfigExtentions
{

    public static void VersionSettings(this PersistenceExtensions<SqlPersistence, StorageType.Sagas> configuration, VersionDeserializeBuilder builder)
    {
        configuration.GetSettings()
            .Set<VersionDeserializeBuilder>(builder);
    }

    internal static VersionDeserializeBuilder GetVersionSettings(this ReadOnlySettings settings)
    {
        return settings.GetOrDefault<VersionDeserializeBuilder>();
    }

}