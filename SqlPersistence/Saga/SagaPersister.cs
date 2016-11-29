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
        var sagaType = context.GetSagaType();
        return Save(sagaData, session, sagaType, correlationProperty?.Value);
    }


    internal async Task Save(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType, object correlationId)
    {
        //TODO: verify SagaCorrelationProperty against our attribute
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType(), sagaType);
        using (var command = new SqlCommand(sagaInfo.SaveCommand, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("OriginalMessageId", DBNullify(sagaData.OriginalMessageId));
            command.AddParameter("Originator", DBNullify(sagaData.Originator));
            command.AddParameter("Data", sagaInfo.ToJson(sagaData));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            if (correlationId != null)
            {
                command.AddParameter("CorrelationId", correlationId);
            }
            AddTransitionalParameter(sagaData, sagaInfo, command);
            await command.ExecuteNonQueryEx();
        }
    }


    static void AddTransitionalParameter(IContainSagaData sagaData, RuntimeSagaInfo sagaInfo, SqlCommand command)
    {
        if (!sagaInfo.HasTransitionalCorrelationProperty)
        {
            return;
        }
        var transitionalId = sagaInfo.TransitionalAccessor(sagaData);
        if (transitionalId == null)
        {
            //TODO:  validate non default for value types
            throw new Exception($"Null transitionalCorrelationProperty is not allowed. SagaDataType: {sagaData.GetType().FullName}.");
        }
        command.AddParameter("TransitionalCorrelationId", transitionalId);
    }


    static object DBNullify(object value)
    {
        return value ?? DBNull.Value;
    }


    public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaType = context.GetSagaType();
        return Update(sagaData, session, sagaType);
    }


    async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType(), sagaType);
        using (var command = new SqlCommand(sagaInfo.UpdateCommand, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("OriginalMessageId", DBNullify(sagaData.OriginalMessageId));
            command.AddParameter("Originator", DBNullify(sagaData.Originator));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            command.Parameters.AddWithValue("Data", sagaInfo.ToJson(sagaData));
            AddTransitionalParameter(sagaData, sagaInfo, command);
            await command.ExecuteNonQueryEx();
        }
    }


    public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        return Get<TSagaData>(sagaId, session, sagaType);
    }


    internal async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, Type sagaType)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
        var sqlSession = session.SqlPersistenceSession();
        using (var command = new SqlCommand(sagaInfo.GetBySagaIdCommand, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("Id", sagaId);
            return await GetSagaData<TSagaData>(command, sagaInfo);
        }
    }


    public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        return Get<TSagaData>(propertyName, propertyValue, session, sagaType);
    }


    internal async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, Type sagaType) 
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);

        if (!sagaInfo.HasCorrelationProperty)
        {
            throw new Exception($"Cannot retrieve a {typeof(TSagaData).FullName} using property \'{propertyName}\'. The saga has no correlation property.");
        }
        if (propertyName != sagaInfo.CorrelationProperty)
        {
            throw new Exception($"Cannot retrieve a {typeof(TSagaData).FullName} using property \'{propertyName}\'. Can only be retrieve using the correlation property '{sagaInfo.CorrelationProperty}'");
        }
        var commandText = sagaInfo.GetByCorrelationPropertyCommand;
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
        using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
        {
            if (!await dataReader.ReadAsync())
            {
                return default(TSagaData);
            }
            var id = dataReader.GetGuid(0);
            var originator = dataReader.GetString(1);
            var originalMessageId = dataReader.GetString(2);
            var sagaTypeVersion = Version.Parse(dataReader.GetString(4));
            using (var textReader = dataReader.GetTextReader(3))
            {
                var sagaData = sagaInfo.FromString<TSagaData>(textReader, sagaTypeVersion);
                sagaData.Id = id;
                sagaData.Originator = originator;
                sagaData.OriginalMessageId = originalMessageId;
                return sagaData;
            }
        }
    }


    public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaType = context.GetSagaType();
        return Complete(sagaData, session, sagaType);
    }


    internal async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType)
    {
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType(), sagaType);
        var sqlSession = session.SqlPersistenceSession();
        using (var command = new SqlCommand(sagaInfo.CompleteCommand, sqlSession.Connection, sqlSession.Transaction))
        {
            command.AddParameter("Id", sagaData.Id);
            await command.ExecuteNonQueryAsync();
        }
    }
}