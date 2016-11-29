using System.Xml;

namespace NServiceBus.Persistence.Sql
{
    public delegate IContainSagaData SagaDeserialize(XmlReader reader);
}