using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NServiceBus.Saga;

static class SagaSerializer
{
    static ConcurrentDictionary<RuntimeTypeHandle, XmlSerializer> serializerCache = new ConcurrentDictionary<RuntimeTypeHandle, XmlSerializer>();
    static XmlSerializerNamespaces emptyNamespace;

    static SagaSerializer()
    {

        emptyNamespace = new XmlSerializerNamespaces();
        emptyNamespace.Add("", "");
    }


    public static string ToXml(IContainSagaData sagaData)
    {
        var serializer = GetSerializar(sagaData.GetType());

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            serializer.Serialize(writer, sagaData, emptyNamespace);
        }
        return builder.ToString();
    }

    public static TSagaData FromString<TSagaData>(XmlReader reader) where TSagaData : IContainSagaData
    {
        var serializer = GetSerializar(typeof (TSagaData));
        return (TSagaData) serializer.Deserialize(reader);
    }

    static XmlSerializer GetSerializar(Type sagaType)
    {
        return serializerCache.GetOrAdd(sagaType.TypeHandle, handle => BuildXmlSerializer(sagaType));
    }

    static XmlSerializer BuildXmlSerializer(Type sagaType)
    {
        var overrides = new XmlAttributeOverrides();
        var ignore = new XmlAttributes { XmlIgnore = true };

        var overrideType = sagaType;
        if (typeof (ContainSagaData).IsAssignableFrom(sagaType))
        {
            overrideType = typeof (ContainSagaData);
        }
        overrides.Add(overrideType, "OriginalMessageId", ignore);
        overrides.Add(overrideType, "Originator", ignore);
        overrides.Add(overrideType, "Id", ignore);
        return new XmlSerializer(
            sagaType, 
            overrides: overrides, 
            extraTypes: new Type[]{}, 
            root: new XmlRootAttribute("Data"), 
            defaultNamespace: ""
            );
    }
}