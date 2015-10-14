using System;

namespace NServiceBus.SqlPersistence
{
    public delegate DefaultSagaSerialization SagaSerializeBuilder(Type sagaDataType);
}