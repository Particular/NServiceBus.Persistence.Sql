using System;
using System.Collections.Concurrent;
using NServiceBus.Persistence.Sql;

class SagaInfoCache
{
    VersionDeserializeBuilder versionDeserializeBuilder;
    SagaSerializeBuilder serializeBuilder;
    SagaCommandBuilder commandBuilder;
    ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo> serializerCache = new ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo>();

    public SagaInfoCache(
        VersionDeserializeBuilder versionDeserializeBuilder,
        SagaSerializeBuilder serializeBuilder,
        SagaCommandBuilder commandBuilder)
    {
        this.versionDeserializeBuilder = versionDeserializeBuilder;
        this.serializeBuilder = serializeBuilder;
        this.commandBuilder = commandBuilder;
    }

    public RuntimeSagaInfo GetInfo(Type sagaDataType, Type sagaType)
    {
        var handle = sagaDataType.TypeHandle;
        return serializerCache.GetOrAdd(handle, _ => BuildSagaInfo(sagaDataType, sagaType));
    }

    RuntimeSagaInfo BuildSagaInfo(Type sagaDataType, Type sagaType)
    {
        var serialization = serializeBuilder(sagaDataType);
        var deserialize = serialization.Deserialize;
        var serialize = serialization.Serialize;
        return new RuntimeSagaInfo(
            commandBuilder: commandBuilder,
            sagaDataType: sagaDataType,
            versionDeserializeBuilder: versionDeserializeBuilder,
            sagaType: sagaType,
            deserialize: deserialize,
            serialize:serialize);
    }
}