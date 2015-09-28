using System;
using System.Collections.Concurrent;
using NServiceBus.SqlPersistence.Saga;

class SagaInfoCache
{
    DeserializeBuilder deserializeBuilder;
    SerializeBuilder serializeBuilder;
    SagaCommandBuilder commandBuilder;
    ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo> serializerCache = new ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo>();

    public SagaInfoCache(DeserializeBuilder deserializeBuilder, SerializeBuilder serializeBuilder,SagaCommandBuilder commandBuilder)
    {
        this.deserializeBuilder = deserializeBuilder;
        this.serializeBuilder = serializeBuilder;
        this.commandBuilder = commandBuilder;
    }

    public RuntimeSagaInfo GetInfo(Type sagaDataType)
    {
        var handle = sagaDataType.TypeHandle;
        return serializerCache.GetOrAdd(handle, _ => BuildSagaInfo(sagaDataType));
    }

    RuntimeSagaInfo BuildSagaInfo(Type sagaDataType)
    {
        return new RuntimeSagaInfo(commandBuilder, sagaDataType, deserializeBuilder, serializeBuilder);
    }
}