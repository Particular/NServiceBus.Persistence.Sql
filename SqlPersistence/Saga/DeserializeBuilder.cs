using System;

namespace NServiceBus.Persistence.Sql
{
    public delegate SagaDeserialize DeserializeBuilder(Type sagaDataType, Version sagaVersion);
}