using System.Xml;

namespace NServiceBus.SqlPersistence
{
    public delegate XmlSagaData SagaDeserialize(XmlReader reader);
}