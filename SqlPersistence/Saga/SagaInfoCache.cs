using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus.Persistence.Sql;

class SagaInfoCache
{
    VersionDeserializeBuilder versionDeserializeBuilder;
    SagaCommandBuilder commandBuilder;
    ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo> serializerCache = new ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo>();
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<StringBuilder, JsonWriter> writerCreator;

    public SagaInfoCache(
        VersionDeserializeBuilder versionDeserializeBuilder,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<StringBuilder, JsonWriter> writerCreator,
        SagaCommandBuilder commandBuilder)
    {
        this.versionDeserializeBuilder = versionDeserializeBuilder;
        this.writerCreator = writerCreator;
        this.readerCreator = readerCreator;
        this.jsonSerializer = jsonSerializer;
        this.commandBuilder = commandBuilder;
    }
    

    public RuntimeSagaInfo GetInfo(Type sagaDataType, Type sagaType)
    {
        var handle = sagaDataType.TypeHandle;
        return serializerCache.GetOrAdd(handle, _ => BuildSagaInfo(sagaDataType, sagaType));
    }

    RuntimeSagaInfo BuildSagaInfo(Type sagaDataType, Type sagaType)
    {
        return new RuntimeSagaInfo(
            commandBuilder: commandBuilder,
            sagaDataType: sagaDataType,
            versionDeserializeBuilder: versionDeserializeBuilder,
            sagaType: sagaType,
            jsonSerializer: jsonSerializer,
            readerCreator: readerCreator,
            writerCreator: writerCreator);
    }
}