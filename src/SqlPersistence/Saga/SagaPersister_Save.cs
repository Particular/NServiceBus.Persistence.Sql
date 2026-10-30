using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

partial class SagaPersister
{
    public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        return Save(sagaData, session, correlationProperty?.Value, cancellationToken);
    }

    internal async Task Save(IContainSagaData sagaData, ISynchronizedStorageSession session, object correlationId, CancellationToken cancellationToken = default)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());

        using (var command = sqlDialect.CreateCommand(sqlSession.Connection))
        {
            command.Transaction = sqlSession.Transaction;
            command.CommandText = sagaInfo.SaveCommand;
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("Metadata", BuildMetadata(sagaData));
            command.AddJsonParameter("Data", sqlDialect.BuildSagaData(command, sagaInfo, sagaData));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            if (correlationId != null)
            {
                command.AddParameter("CorrelationId", correlationId);
            }
            AddTransitionalParameter(sagaData, sagaInfo, command);
            await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
        }
    }
}