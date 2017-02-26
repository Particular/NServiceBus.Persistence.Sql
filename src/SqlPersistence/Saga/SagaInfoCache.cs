using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using NServiceBus.Persistence.Sql;
#pragma warning disable 618

class SagaInfoCache
{
    RetrieveVersionSpecificJsonSettings versionSpecificSettings;
    SagaCommandBuilder commandBuilder;
    ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo> serializerCache = new ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo>();
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<TextWriter, JsonWriter> writerCreator;

    public SagaInfoCache(
        RetrieveVersionSpecificJsonSettings versionSpecificSettings,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<TextWriter, JsonWriter> writerCreator,
        SagaCommandBuilder commandBuilder)
    {
        this.versionSpecificSettings = versionSpecificSettings;
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
            versionSpecificSettings: versionSpecificSettings,
            sagaType: sagaType,
            jsonSerializer: jsonSerializer,
            readerCreator: readerCreator,
            writerCreator: writerCreator);
    }
}