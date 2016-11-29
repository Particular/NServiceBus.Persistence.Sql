using System;

namespace NServiceBus.Persistence.Sql
{
    public delegate DefaultSagaSerialization<TReader> SagaSerializeBuilder<TReader>(Type sagaDataType);
}