using System.IO;
using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence
{
    public delegate void SagaSerialize(StringWriter reader, IContainSagaData sagaData);
}