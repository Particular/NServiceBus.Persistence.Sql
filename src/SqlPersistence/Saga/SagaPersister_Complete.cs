using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{
    public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) =>
        Complete(sagaData, session, GetConcurrency(context), cancellationToken);

    internal async Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, int concurrency, CancellationToken cancellationToken = default)
    {
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());
        var sqlSession = session.SqlPersistenceSession();

        using (var command = sqlDialect.CreateCommand(sqlSession.Connection))
        {
            command.CommandText = sagaInfo.CompleteCommand;
            command.Transaction = sqlSession.Transaction;

            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("Concurrency", concurrency);

            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            if (affected != 1)
            {
                throw new Exception($"Optimistic concurrency violation when trying to complete saga {sagaInfo.SagaType.FullName} {sagaData.Id}. Expected version {concurrency}.");
            }
        }
    }
}