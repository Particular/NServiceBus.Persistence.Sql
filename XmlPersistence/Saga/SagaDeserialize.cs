using System.Xml;

namespace NServiceBus.Persistence.SqlServerXml
{
    public delegate XmlSagaData SagaDeserialize(XmlReader reader);
}