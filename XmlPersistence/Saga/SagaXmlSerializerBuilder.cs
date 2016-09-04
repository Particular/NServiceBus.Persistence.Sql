using System;
using System.Xml.Serialization;
using NServiceBus;
using NServiceBus.Persistence.Sql.Xml;
using XmlSerializer = System.Xml.Serialization.XmlSerializer;

static class SagaXmlSerializerBuilder
{

    static XmlSerializerNamespaces emptyNamespace;

    static SagaXmlSerializerBuilder()
    {
        emptyNamespace = new XmlSerializerNamespaces();
        emptyNamespace.Add("", "");
    }

    public static DefaultSagaSerialization BuildSerializationDelegate(Type sagaDataType,
        Action<XmlSerializer, Type> customize)
    {
        var xmlSerializer = BuildXmlSerializer(sagaDataType, customize);
        return new DefaultSagaSerialization
            (
            (writer, data) =>
            {
                xmlSerializer.Serialize(writer, data, emptyNamespace);
            },
            reader => (IContainSagaData) xmlSerializer.Deserialize(reader)
            );
    }

    static XmlSerializer BuildXmlSerializer(Type sagaDataType, Action<XmlSerializer, Type> customize)
    {
        //TODO: if sagaDataType is not public instruct developer to override xml serialization or make type public
        var xmlAttributeOverrides = new XmlAttributeOverrides();
        var containSagaData = typeof(ContainSagaData);
        var overrideType = sagaDataType;
        if (containSagaData.IsAssignableFrom(sagaDataType))
        {
            overrideType = containSagaData;
        }
        xmlAttributeOverrides.Add(overrideType, "Id", new XmlAttributes {XmlIgnore = true});
        xmlAttributeOverrides.Add(overrideType, "Originator", new XmlAttributes {XmlIgnore = true});
        xmlAttributeOverrides.Add(overrideType, "OriginalMessageId", new XmlAttributes {XmlIgnore = true});
        var xmlSerializer = new XmlSerializer(
            sagaDataType,
            overrides: xmlAttributeOverrides,
            extraTypes: new Type[] {},
            root: new XmlRootAttribute("Data"),
            defaultNamespace: ""
            );
        customize(xmlSerializer, sagaDataType);
        return xmlSerializer;
    }
}