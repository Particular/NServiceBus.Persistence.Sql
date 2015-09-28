using System.Xml;
using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence.Saga
{
    public delegate IContainSagaData Deserialize(XmlReader reader);
}