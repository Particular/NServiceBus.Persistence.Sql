using System;
using System.Xml.Serialization;
using NServiceBus.SqlPersistence;

static class SagaXmlSerializerBuilder
{
    
    static XmlSerializerNamespaces emptyNamespace;

    static SagaXmlSerializerBuilder()
    {
        emptyNamespace = new XmlSerializerNamespaces();
        emptyNamespace.Add("", "");
    }
    public static DefaultSagaSerialization BuildSerializationDelegate(Type sagaDataType, Action<XmlSerializer, Type> customize)
    {
        var xmlSerializer = BuildXmlSerializer(sagaDataType, customize);
        return new DefaultSagaSerialization
            (
            (writer, data) =>
            {
                xmlSerializer.Serialize(writer, data, emptyNamespace);
            },
            reader => (XmlSagaData) xmlSerializer.Deserialize(reader)
            );
    }

    static XmlSerializer BuildXmlSerializer(Type sagaDataType, Action<XmlSerializer, Type> customize)
    {
        var xmlSerializer = new XmlSerializer(
            sagaDataType, 
            overrides: new XmlAttributeOverrides(), 
            extraTypes: new Type[]{}, 
            root: new XmlRootAttribute("Data"), 
            defaultNamespace: ""
            );
        customize(xmlSerializer, sagaDataType);
        return xmlSerializer;
    }
}