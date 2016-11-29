using System;

namespace NServiceBus.Persistence.Sql
{
    public delegate SagaDeserialize<TReader> VersionDeserializeBuilder<TReader>(Type sagaDataType, Version sagaVersion);
}