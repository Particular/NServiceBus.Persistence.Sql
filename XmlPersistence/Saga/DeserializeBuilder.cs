using System;

namespace NServiceBus.Persistence.Sql.Xml
{
    public delegate SagaDeserialize DeserializeBuilder(Type sagaDataType, Version sagaVersion);
}