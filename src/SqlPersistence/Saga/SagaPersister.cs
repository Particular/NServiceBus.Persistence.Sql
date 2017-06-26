using System;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;
using NServiceBus.Sagas;

partial class SagaPersister : ISagaPersister
{
    SagaInfoCache sagaInfoCache;
    CommandBuilder commandBuilder;

    public SagaPersister(SagaInfoCache sagaInfoCache, SqlVariant sqlVariant)
    {
        this.sagaInfoCache = sagaInfoCache;
        commandBuilder = new CommandBuilder(sqlVariant);
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

    internal struct Concurrency<TSagaData>
        where TSagaData : IContainSagaData
    {
        public readonly TSagaData Data;
        public readonly int Version;

        public Concurrency(TSagaData data, int version)
        {
            Data = data;
            Version = version;
        }
    }
}