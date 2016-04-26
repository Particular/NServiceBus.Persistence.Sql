using System.Xml;

namespace NServiceBus.Persistence.SqlServerXml
{
    public delegate IContainSagaData SagaDeserialize(XmlReader reader);
}