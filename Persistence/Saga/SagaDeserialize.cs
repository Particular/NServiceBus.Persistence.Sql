using System.Xml;
using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence
{
    public delegate IContainSagaData SagaDeserialize(XmlReader reader);
}