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
    ConcurrentDictionary<Type, RuntimeSagaInfo> cache = new ConcurrentDictionary<Type, RuntimeSagaInfo>();
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<TextWriter, JsonWriter> writerCreator;
    Func<string, string> nameFilter;
    string tablePrefix;
    bool usesOptimisticConcurrency;
    SqlDialect sqlDialect;

    public SagaInfoCache(
        RetrieveVersionSpecificJsonSettings versionSpecificSettings,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<TextWriter, JsonWriter> writerCreator,
        string tablePrefix,
        bool usesOptimisticConcurrency,
        SqlDialect sqlDialect,
        SagaMetadataCollection metadataCollection,
        Func<string, string> nameFilter)
    {
        this.versionSpecificSettings = versionSpecificSettings;
        this.writerCreator = writerCreator;
        this.readerCreator = readerCreator;
        this.jsonSerializer = jsonSerializer;
        this.tablePrefix = tablePrefix;
        this.usesOptimisticConcurrency = usesOptimisticConcurrency;
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
            var sagaInfo = BuildSagaInfo(sagaDataType, metadata);
            cache[sagaDataType] = sagaInfo;
            if (sagaInfo.CorrelationProperty != null && !metadata.TryGetCorrelationProperty(out var _))
            {
                throw new Exception($"The saga '{metadata.SagaType.FullName}' defines a correlation property '{sagaInfo.CorrelationProperty}' which is not mapped to any message. Either map it or remove it from the saga definition.");
            }
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

    RuntimeSagaInfo BuildSagaInfo(Type sagaDataType, SagaMetadata metadata)
    {
        return new RuntimeSagaInfo(
            sagaDataType: sagaDataType,
            versionSpecificSettings: versionSpecificSettings,
            metadata: metadata,
            jsonSerializer: jsonSerializer,
            readerCreator: readerCreator,
            writerCreator: writerCreator,
            tablePrefix: tablePrefix,
            usesOptimisticConcurrency: usesOptimisticConcurrency,
            sqlDialect: sqlDialect,
            nameFilter: nameFilter);
    }
}