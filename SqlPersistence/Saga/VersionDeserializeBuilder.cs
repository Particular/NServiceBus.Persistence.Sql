using System;

namespace NServiceBus.Persistence.Sql
{
    public delegate SagaDeserialize VersionDeserializeBuilder(Type sagaDataType, Version sagaVersion);
}