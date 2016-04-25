using System;

namespace NServiceBus.Persistence.SqlServerXml
{
    public delegate SagaDeserialize DeserializeBuilder(Type sagaDataType, Version sagaVersion);
}