using System.Xml;

namespace NServiceBus.Persistence.Sql.Xml
{
    public delegate IContainSagaData SagaDeserialize(XmlReader reader);
}