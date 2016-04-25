using System;

namespace NServiceBus.Persistence.SqlServerXml
{
    public delegate DefaultSagaSerialization SagaSerializeBuilder(Type sagaDataType);
}