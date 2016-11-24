using System;
using System.Collections.Concurrent;
using System.Xml.Serialization;
using NServiceBus.Persistence.Sql;

class SagaInfoCache
{
    DeserializeBuilder deserializeBuilder;
    SagaSerializeBuilder serializeBuilder;
    SagaCommandBuilder commandBuilder;
    Action<XmlSerializer, Type> xmlSerializerCustomize;
    ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo> serializerCache = new ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo>();

    public SagaInfoCache(
        DeserializeBuilder deserializeBuilder,
        SagaSerializeBuilder serializeBuilder,
        SagaCommandBuilder commandBuilder,
        Action<XmlSerializer, Type> xmlSerializerCustomize)
    {
        this.deserializeBuilder = deserializeBuilder;
        this.serializeBuilder = serializeBuilder;
        this.commandBuilder = commandBuilder;
        this.xmlSerializerCustomize = xmlSerializerCustomize;
    }

    public RuntimeSagaInfo GetInfo(Type sagaDataType, Type sagaType)
    {
        var handle = sagaDataType.TypeHandle;
        return serializerCache.GetOrAdd(handle, _ => BuildSagaInfo(sagaDataType, sagaType));
    }

    RuntimeSagaInfo BuildSagaInfo(Type sagaDataType, Type sagaType)
    {
        return new RuntimeSagaInfo(commandBuilder, sagaDataType, deserializeBuilder, serializeBuilder, xmlSerializerCustomize, sagaType);
    }
}