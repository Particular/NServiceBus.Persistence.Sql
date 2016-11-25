using System;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.Sql;
using XmlSerializer = System.Xml.Serialization.XmlSerializer;

public static class SagaConfigExtentions
{
    public static void XmlSerializerCustomize(this PersistenceExtensions<SqlXmlPersistence, StorageType.Sagas> configuration, Action<XmlSerializer, Type> xmlSerializerCustomize)
    {
        configuration.GetSettings()
            .Set("SqlPersistence.XmlSerializerCustomize", xmlSerializerCustomize);
    }

    internal static Action<XmlSerializer, Type> GetXmlSerializerCustomize(this ReadOnlySettings settings)
    {
        Action<XmlSerializer, Type> value;
        if (settings.TryGet("SqlPersistence.XmlSerializerCustomize", out value))
        {
            return value;
        }
        return (serializer, type) => {};
    }

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

    public static void DeserializeBuilder(this PersistenceExtensions<SqlXmlPersistence, StorageType.Sagas> configuration, VersionDeserializeBuilder builder)
    {
        configuration.GetSettings()
            .Set<VersionDeserializeBuilder>(builder);
    }

    internal static VersionDeserializeBuilder GetDeserializeBuilder(this ReadOnlySettings settings)
    {
        VersionDeserializeBuilder value;
        settings.TryGet(out value);
        return value;
    }

}