using System;

namespace NServiceBus.SqlPersistence.Saga
{
    public delegate Deserialize DeserializeBuilder(Type sagaDataType, Version sagaVersion);
}