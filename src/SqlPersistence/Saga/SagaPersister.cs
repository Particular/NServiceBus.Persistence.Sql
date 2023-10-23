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

        //TODO: validate non default for value types if TransitionalAccessor returns null
        var transitionalId = sagaInfo.TransitionalAccessor(sagaData) ?? throw new Exception($"Null transitionalCorrelationProperty is not allowed. SagaDataType: {sagaData.GetType().FullName}.");
        command.AddParameter("TransitionalCorrelationId", transitionalId, 200);
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