using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{
    public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        return Update(sagaData, session, GetConcurrency(context), cancellationToken);
    }

    internal async Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, int concurrency, CancellationToken cancellationToken = default)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());

        using (var command = sagaInfo.CreateCommand(sqlSession.Connection))
        {
            command.CommandText = sagaInfo.UpdateCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaData.Id, 200);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion, 23);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion, 23);
            command.AddJsonParameter("Data", sqlDialect.BuildSagaData(command, sagaInfo, sagaData));
            command.AddParameter("Concurrency", concurrency, 0);
            AddTransitionalParameter(sagaData, sagaInfo, command);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (affected != 1)
            {
                throw new Exception($"Optimistic concurrency violation when trying to save saga {sagaInfo.SagaType.FullName} {sagaData.Id}. Expected version {concurrency}.");
            }
        }
    }
}