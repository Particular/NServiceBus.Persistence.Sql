
using System.Data.SqlClient;
using System.Xml;

namespace NServiceBus.Persistence.Sql
{
    public class XmlPersistenceSerializer : SqlPersistenceSerializer<XmlReader>
    {

        public XmlReader GetReader(SqlDataReader reader, int column)
        {
            return reader.GetSqlXml(column).CreateReader();
        }

        public void SetSerializeBuilder(SagaSerializeBuilder<XmlReader> defaultSagaSerialization)
        {
            SerializationBuilder = defaultSagaSerialization;
            if (SerializationBuilder == null)
            {
                SerializationBuilder = SagaXmlSerializerBuilder.BuildSerializationDelegate;
            }
        }

        public SagaSerializeBuilder<XmlReader> SerializationBuilder { get; private set; }

        public void SetVersionDeserializeBuilder(VersionDeserializeBuilder<XmlReader> versionDeserializeBuilder)
        {
            VersionDeserializeBuilder = versionDeserializeBuilder;
        }

        public VersionDeserializeBuilder<XmlReader> VersionDeserializeBuilder { get; private set; }
    }
}