using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

partial class SagaPersister
{
    public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
    {
        var metadata = GetMetadata(sagaData, context);
        return Save(sagaData, session, correlationProperty?.Value, metadata);
    }

    internal async Task Save(IContainSagaData sagaData, SynchronizedStorageSession session, object correlationId, SagaInstanceMetadata metadata)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());

        using (var command = sqlDialect.CreateCommand(sqlSession.Connection))
        {
            command.Transaction = sqlSession.Transaction;
            command.CommandText = sagaInfo.SaveCommand;
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("Metadata", Serializer.Serialize(metadata));
            command.AddJsonParameter("Data", sqlDialect.BuildSagaData(command, sagaInfo, sagaData));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            if (correlationId != null)
            {
                command.AddParameter("CorrelationId", correlationId);
            }
            AddTransitionalParameter(sagaData, sagaInfo, command);
            await command.ExecuteNonQueryEx().ConfigureAwait(false);
        }
    }
}