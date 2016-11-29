using System;
using System.Collections.Concurrent;
using NServiceBus.Persistence.Sql;

class SagaInfoCache<TReader>
    where TReader : IDisposable
{
    SagaCommandBuilder commandBuilder;
    SqlPersistenceSerializer<TReader> sqlPersistenceSerializer;
    ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo<TReader>> serializerCache = new ConcurrentDictionary<RuntimeTypeHandle, RuntimeSagaInfo<TReader>>();

    public SagaInfoCache(SagaCommandBuilder commandBuilder, SqlPersistenceSerializer<TReader> sqlPersistenceSerializer)
    {
        this.commandBuilder = commandBuilder;
        this.sqlPersistenceSerializer = sqlPersistenceSerializer;
    }

    public RuntimeSagaInfo<TReader> GetInfo(Type sagaDataType, Type sagaType)
    {
        var handle = sagaDataType.TypeHandle;
        return serializerCache.GetOrAdd(handle, _ => BuildSagaInfo(sagaDataType, sagaType));
    }

    RuntimeSagaInfo<TReader> BuildSagaInfo(Type sagaDataType, Type sagaType)
    {
        return new RuntimeSagaInfo<TReader>(
            commandBuilder: commandBuilder,
            sagaDataType: sagaDataType,
            sagaType: sagaType, 
            sqlPersistenceSerializer: sqlPersistenceSerializer);
    }
}