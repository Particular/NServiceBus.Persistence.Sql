using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

#pragma warning disable 618

class SagaInfoCache
{
    RetrieveVersionSpecificJsonSettings versionSpecificSettings;
    SagaCommandBuilder commandBuilder;
    ConcurrentDictionary<Type, RuntimeSagaInfo> cache = new ConcurrentDictionary<Type, RuntimeSagaInfo>();
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<TextWriter, JsonWriter> writerCreator;
    Func<string, string> nameFilter;
    string tablePrefix;
    SqlDialect sqlDialect;

    public SagaInfoCache(
        RetrieveVersionSpecificJsonSettings versionSpecificSettings,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<TextWriter, JsonWriter> writerCreator,
        SagaCommandBuilder commandBuilder,
        string tablePrefix,
        SqlDialect sqlDialect,
        SagaMetadataCollection metadataCollection,
        Func<string, string> nameFilter)
    {
        this.versionSpecificSettings = versionSpecificSettings;
        this.writerCreator = writerCreator;
        this.readerCreator = readerCreator;
        this.jsonSerializer = jsonSerializer;
        this.commandBuilder = commandBuilder;
        this.tablePrefix = tablePrefix;
        this.sqlDialect = sqlDialect;
        this.nameFilter = nameFilter;
        Initialize(metadataCollection);
    }

    void Initialize(SagaMetadataCollection metadataCollection)
    {
        foreach (var metadata in metadataCollection)
        {
            var sagaDataType = metadata.SagaEntityType;
            if (cache.TryGetValue(sagaDataType, out var existing))
            {
                throw new Exception($"The saga data '{sagaDataType.FullName}' is being used by both '{existing.SagaType}' and '{metadata.SagaType.FullName}'. Saga data can only be used by one saga.");
            }
            cache[sagaDataType] = BuildSagaInfo(sagaDataType, metadata.SagaType);
        }
    }

    public RuntimeSagaInfo GetInfo(Type sagaDataType)
    {
        if (cache.TryGetValue(sagaDataType, out var value))
        {
            return value;
        }
        throw new Exception($"Could not find RuntimeSagaInfo for {sagaDataType.FullName}.");
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
            writerCreator: writerCreator,
            tablePrefix: tablePrefix,
            sqlDialect: sqlDialect,
            nameFilter: nameFilter);
    }
}