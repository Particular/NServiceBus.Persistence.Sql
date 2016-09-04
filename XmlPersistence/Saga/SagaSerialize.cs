using System.IO;

namespace NServiceBus.Persistence.Sql.Xml
{
    public delegate void SagaSerialize(StringWriter reader, IContainSagaData sagaData);
}