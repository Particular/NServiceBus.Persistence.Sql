using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{

    public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        return Complete(sagaData, session, GetConcurrency(context));
    }

    internal async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, int concurrency)
    {
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());
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