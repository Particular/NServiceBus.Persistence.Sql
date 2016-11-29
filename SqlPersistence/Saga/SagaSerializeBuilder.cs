using System;

namespace NServiceBus.Persistence.Sql
{
    public delegate DefaultSagaSerialization SagaSerializeBuilder(Type sagaDataType);
}