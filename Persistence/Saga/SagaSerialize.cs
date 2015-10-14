using System.IO;

namespace NServiceBus.SqlPersistence
{
    public delegate void SagaSerialize(StringWriter reader, XmlSagaData sagaData);
}