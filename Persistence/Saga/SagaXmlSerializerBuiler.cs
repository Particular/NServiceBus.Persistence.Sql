using System;
using System.Xml.Serialization;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence.Saga;

static class SagaXmlSerializerBuiler
{
    static XmlSerializerNamespaces emptyNamespace;

    static SagaXmlSerializerBuiler()
    {
        emptyNamespace = new XmlSerializerNamespaces();
        emptyNamespace.Add("", "");
    }

    public static DefualtSerialization BuildSerializationDelegate(Type sagaType)
    {
        var xmlSerializer = BuildXmlSerializer(sagaType);
        return new DefualtSerialization
            (
            (writer, data) =>
            {
                xmlSerializer.Serialize(writer, data, emptyNamespace);
            },
            reader => (IContainSagaData) xmlSerializer.Deserialize(reader)
            );
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