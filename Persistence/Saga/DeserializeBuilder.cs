using System;

namespace NServiceBus.SqlPersistence
{
    public delegate SagaDeserialize DeserializeBuilder(Type sagaDataType, Version sagaVersion);
}