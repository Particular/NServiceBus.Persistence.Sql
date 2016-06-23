using System;
using NServiceBus;
using NServiceBus.Persistence;
using NServiceBus.Settings;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Persistence.SqlServerXml;
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

    public static void DeserializeBuilder(this PersistenceExtensions<SqlXmlPersistence, StorageType.Sagas> configuration, DeserializeBuilder builder)
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