using System;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Sagas;

partial class SagaPersister : ISagaPersister
{
    SagaInfoCache sagaInfoCache;
    SqlDialect sqlDialect;

    public SagaPersister(SagaInfoCache sagaInfoCache, SqlDialect sqlDialect)
    {
        this.sagaInfoCache = sagaInfoCache;
        this.sqlDialect = sqlDialect;
    }

    static void AddTransitionalParameter(IContainSagaData sagaData, RuntimeSagaInfo sagaInfo, CommandWrapper command)
    {
        if (!sagaInfo.HasTransitionalCorrelationProperty)
        {
            return;
        }
        var transitionalId = sagaInfo.TransitionalAccessor(sagaData);
        if (transitionalId == null)
        {
            //TODO: validate non default for value types
            throw new Exception($"Null transitionalCorrelationProperty is not allowed. SagaDataType: {sagaData.GetType().FullName}.");
        }
        command.AddParameter("TransitionalCorrelationId", transitionalId);
    }

    static int GetConcurrency(ContextBag context)
    {
        if (!context.TryGet("NServiceBus.Persistence.Sql.Concurrency", out int concurrency))
        {
            throw new Exception("Cannot save saga because optimistic concurrency version is missing in the context.");
        }
        return concurrency;
    }

    SagaInstanceMetadata GetMetadata(IContainSagaData sagaData, ContextBag context)
    {
        if (!context.TryGet(out SagaInstanceMetadata metadata))
        {
            metadata = new SagaInstanceMetadata();
        }
        if (sagaData.OriginalMessageId != null)
        {
            metadata.OriginalMessageId = sagaData.OriginalMessageId;
        }
        if (sagaData.Originator != null)
        {
            metadata.Originator = sagaData.Originator;
        }
        return metadata;
    }

    internal struct ConcurrencyAndMetadata<TSagaData>
        where TSagaData : IContainSagaData
    {
        public readonly TSagaData Data;
        public readonly SagaInstanceMetadata Metadata;
        public readonly int Version;

        public ConcurrencyAndMetadata(TSagaData data, int version, SagaInstanceMetadata metadata)
        {
            Data = data;
            Version = version;
            Metadata = metadata;
        }
    }
}