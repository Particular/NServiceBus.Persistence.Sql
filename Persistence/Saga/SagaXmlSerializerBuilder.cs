using System;
using System.Xml.Serialization;
using NServiceBus.Saga;
using NServiceBus.SqlPersistence;
using NServiceBus.SqlPersistence.Saga;

static class SagaXmlSerializerBuilder
{
    
    public static DefualtSerialization BuildSerializationDelegate(Type sagaDataType, Action<XmlSerializer, Type> customize)
    {
        var xmlSerializer = BuildXmlSerializer(sagaDataType, customize);
        return new DefualtSerialization
            (
            (writer, data) =>
            {
                xmlSerializer.Serialize(writer, data, XmlOverrideBuilder.EmptyNamespace);
            },
            reader => (IContainSagaData) xmlSerializer.Deserialize(reader)
            );
    }

    static XmlSerializer BuildXmlSerializer(Type sagaDataType, Action<XmlSerializer, Type> customize)
    {
        var overrides = XmlOverrideBuilder.BuildAttributeOverrides(sagaDataType);
        var xmlSerializer = new XmlSerializer(
            sagaDataType, 
            overrides: overrides, 
            extraTypes: new Type[]{}, 
            root: new XmlRootAttribute("Data"), 
            defaultNamespace: ""
            );
        customize(xmlSerializer, sagaDataType);
        return xmlSerializer;
    }
}