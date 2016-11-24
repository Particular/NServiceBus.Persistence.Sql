using System.IO;

namespace NServiceBus.Persistence.Sql
{
    public delegate void SagaSerialize(StringWriter reader, IContainSagaData sagaData);
}