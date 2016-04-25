using System.IO;

namespace NServiceBus.Persistence.SqlServerXml
{
    public delegate void SagaSerialize(StringWriter reader, XmlSagaData sagaData);
}