using System;

namespace NServiceBus.Persistence.Sql.Xml
{
    public delegate DefaultSagaSerialization SagaSerializeBuilder(Type sagaDataType);
}