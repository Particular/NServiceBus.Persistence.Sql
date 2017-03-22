using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{

    public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaType = context.GetSagaType();
        return Update(sagaData, session, sagaType, GetConcurrency(context));
    }

    internal async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType, int concurrency)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType(), sagaType);

        using (var command = sagaInfo.CreateCommand(sqlSession.Connection))
        {
            command.CommandText = sagaInfo.UpdateCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            command.AddParameter("Data", sagaInfo.ToJson(sagaData));
            command.AddParameter("Concurrency", concurrency);
            AddTransitionalParameter(sagaData, sagaInfo, command.InnerCommand);
            var affected = await command.ExecuteNonQueryAsync();
            if (affected != 1)
            {
                throw new Exception($"Optimistic concurrency violation when trying to save saga {sagaType.FullName} {sagaData.Id}. Expected version {concurrency}.");
            }
        }
    }

}