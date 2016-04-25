using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

class SagaPersister : ISagaPersister
{
    SagaInfoCache sagaInfoCache;

    public SagaPersister(SagaInfoCache sagaInfoCache)
    {
        this.sagaInfoCache = sagaInfoCache;
    }

    public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
    {
        var sqlSession = session.SqlPersistenceSession();
        return Save(sagaData, sqlSession.Connection, sqlSession.Transaction);
    }


    internal async Task Save(IContainSagaData sagaData, SqlConnection sqlConnection, SqlTransaction sqlTransaction)
    {
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());
        using (var command = new SqlCommand(sagaInfo.SaveCommand, sqlConnection, sqlTransaction))
        {
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("OriginalMessageId", sagaData.OriginalMessageId);
            command.AddParameter("Originator", sagaData.Originator);
            command.AddParameter("Data", sagaInfo.ToXml(sagaData));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());
        using (var command = new SqlCommand(sagaInfo.UpdateCommand, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("OriginalMessageId", sagaData.OriginalMessageId);
            command.AddParameter("Originator", sagaData.Originator);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            command.Parameters.AddWithValue("Data", sagaInfo.ToXml(sagaData));
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof (TSagaData));
        var sqlSession = session.SqlPersistenceSession();
        using (var command = new SqlCommand(sagaInfo.GetBySagaIdCommand, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("Id", sagaId);
            return await GetSagaData<TSagaData>(command, sagaInfo);
        }
    }

    public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof (TSagaData));
        var commandText = sagaInfo.GetMappedPropertyCommand(propertyName);
        var sqlSession = session.SqlPersistenceSession();
        using (var command = new SqlCommand(commandText, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("propertyValue", propertyValue.ToString());
            return await GetSagaData<TSagaData>(command, sagaInfo);
        }
    }

    static async Task<TSagaData> GetSagaData<TSagaData>(SqlCommand command, RuntimeSagaInfo sagaInfo)
        where TSagaData : IContainSagaData
    {
        using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
        {
            if (!await reader.ReadAsync())
            {
                return default(TSagaData);
            }
            var id = reader.GetGuid(0);
            var originator = reader.GetString(1);
            var originalMessageId = reader.GetString(2);
            var sagaTypeVersion = Version.Parse(reader.GetString(4));
            using (var xmlReader = reader.GetSqlXml(3).CreateReader())
            {
                var sagaData = sagaInfo.FromString<TSagaData>(xmlReader, sagaTypeVersion);
                sagaData.Id = id;
                sagaData.Originator = originator;
                sagaData.OriginalMessageId = originalMessageId;
                return sagaData;
            }
        }
    }

    public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType());
        var sqlSession = session.SqlPersistenceSession();
        using (var command = new SqlCommand(sagaInfo.CompleteCommand, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("Id", sagaData.Id);
            await command.ExecuteNonQueryAsync();
        }
    }

}