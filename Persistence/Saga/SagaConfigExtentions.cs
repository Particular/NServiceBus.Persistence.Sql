using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.SqlPersistence.Saga;

public static class SagaConfigExtentions
{
    public static void SerializeBuilder(this PersistenceExtentions<SqlPersistence, StorageType.Sagas> configuration, SerializeBuilder builder)
    {
        configuration.GetSettings()
            .Set<SerializeBuilder>(builder);
    }

    internal static SerializeBuilder GetSerializeBuilder(this ReadOnlySettings settings)
    {
        SerializeBuilder value;
        settings.TryGet(out value);
        return value;
    }

    public static void DeserializeBuilder(this PersistenceExtentions<SqlPersistence, StorageType.Sagas> configuration, DeserializeBuilder builder)
    {
        configuration.GetSettings()
            .Set<DeserializeBuilder>(builder);
    }

    internal static DeserializeBuilder GetDeserializeBuilder(this ReadOnlySettings settings)
    {
        DeserializeBuilder value;
        settings.TryGet(out value);
        return value;
    }

}