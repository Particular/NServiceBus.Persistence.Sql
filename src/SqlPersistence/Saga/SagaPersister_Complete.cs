using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{

    public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaType = context.GetSagaType();
        return Complete(sagaData, session, sagaType, GetConcurrency(context));
    }

    internal async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType, int concurrency)
    {
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType(), sagaType);
        var sqlSession = session.SqlPersistenceSession();

        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = sagaInfo.CompleteCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("Concurrency", concurrency);
            await command.ExecuteNonQueryAsync();
        }
    }
}