using System;

namespace NServiceBus.SqlPersistence.Saga
{
    public delegate DefualtSerialization SerializeBuilder(Type sagaDataType);
}