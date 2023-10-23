using System.Collections.Generic;
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
            command.AddParameter("Id", sagaData.Id, 200);
            var metadata = new Dictionary<string, string>();
            if (sagaData.OriginalMessageId != null)
            {
                metadata.Add("OriginalMessageId", sagaData.OriginalMessageId);
            }
            if (sagaData.Originator != null)
            {
                metadata.Add("Originator", sagaData.Originator);
            }
            command.AddParameter("Metadata", Serializer.Serialize(metadata), -1);
            command.AddJsonParameter("Data", sqlDialect.BuildSagaData(command, sagaInfo, sagaData));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion, 23);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion, 23);
            if (correlationId != null)
            {
                command.AddParameter("CorrelationId", correlationId, 200);
            }
            AddTransitionalParameter(sagaData, sagaInfo, command);
            await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
        }
    }
}