using System;
using System.Xml.Serialization;

namespace NServiceBus.Persistence.Sql
{
   public  static class SagaXmlSerializerBuilder
    {

        static XmlSerializerNamespaces emptyNamespace;

        static SagaXmlSerializerBuilder()
        {
            emptyNamespace = new XmlSerializerNamespaces();
            emptyNamespace.Add("", "");
        }

        public static DefaultSagaSerialization BuildSerializationDelegate(Type sagaDataType)
        {
            var xmlSerializer = BuildXmlSerializer(sagaDataType);
            return new DefaultSagaSerialization
            (
                (writer, data) =>
                {
                    xmlSerializer.Serialize(writer, data, emptyNamespace);
                },
                reader => (IContainSagaData) xmlSerializer.Deserialize(reader)
            );
        }

        public static System.Xml.Serialization.XmlSerializer BuildXmlSerializer(Type sagaDataType)
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
            return new System.Xml.Serialization.XmlSerializer(
                sagaDataType,
                overrides: xmlAttributeOverrides,
                extraTypes: new Type[] {},
                root: new XmlRootAttribute("Data"),
                defaultNamespace: ""
            );
        }
    }
}