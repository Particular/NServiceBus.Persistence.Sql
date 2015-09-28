using System.IO;
using NServiceBus.Saga;

namespace NServiceBus.SqlPersistence.Saga
{
    public delegate void Serialize(StringWriter reader, IContainSagaData sagaData);
}