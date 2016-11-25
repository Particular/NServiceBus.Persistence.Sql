using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;

public static class SagaConfigExtentions
{
 
    public static void SerializeBuilder(this PersistenceExtensions<SqlXmlPersistence, StorageType.Sagas> configuration, SagaSerializeBuilder builder)
    {
        configuration.GetSettings()
            .Set<SagaSerializeBuilder>(builder);
    }

    internal static SagaSerializeBuilder GetSerializeBuilder(this ReadOnlySettings settings)
    {
        SagaSerializeBuilder value;
        settings.TryGet(out value);
        return value;
    }

    public static void VersionDeserializeBuilder(this PersistenceExtensions<SqlXmlPersistence, StorageType.Sagas> configuration, VersionDeserializeBuilder builder)
    {
        configuration.GetSettings()
            .Set<VersionDeserializeBuilder>(builder);
    }

    internal static VersionDeserializeBuilder GetVersionDeserializeBuilder(this ReadOnlySettings settings)
    {
        VersionDeserializeBuilder value;
        settings.TryGet(out value);
        return value;
    }

}