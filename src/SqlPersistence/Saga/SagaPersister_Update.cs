using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{
    public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var metadata = GetMetadata(sagaData, context);
        return Update(sagaData, session, GetConcurrency(context), metadata);
    }

    internal async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, int concurrency, SagaInstanceMetadata metadata)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());

        using (var command = sagaInfo.CreateCommand(sqlSession.Connection))
        {
            command.CommandText = sagaInfo.UpdateCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            command.AddJsonParameter("Data", sqlDialect.BuildSagaData(command, sagaInfo, sagaData));
            command.AddParameter("Metadata", Serializer.Serialize(metadata));
            command.AddParameter("Concurrency", concurrency);
            AddTransitionalParameter(sagaData, sagaInfo, command);
            var affected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            if (affected != 1)
            {
                throw new Exception($"Optimistic concurrency violation when trying to save saga {sagaInfo.SagaType.FullName} {sagaData.Id}. Expected version {concurrency}.");
            }
        }
    }
}