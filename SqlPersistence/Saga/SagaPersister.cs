using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.Transaction = sqlSession.Transaction;
            command.CommandText = sagaInfo.SaveCommand;
            command.AddParameter("Id", sagaData.Id);
            var metadata = new Dictionary<string, string>();
            if (sagaData.OriginalMessageId != null)
            {
                metadata.Add("OriginalMessageId", sagaData.OriginalMessageId);
            }
            if (sagaData.Originator != null)
            {
                metadata.Add("Originator", sagaData.Originator);
            }
            command.AddParameter("Metadata", Serializer.Serialize(metadata));
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


    static void AddTransitionalParameter(IContainSagaData sagaData, RuntimeSagaInfo sagaInfo, DbCommand command)
    {
        if (!sagaInfo.HasTransitionalCorrelationProperty)
        {
            return;
        }
        var transitionalId = sagaInfo.TransitionalAccessor(sagaData);
        if (transitionalId == null)
        {
            //TODO: validate non default for value types
            throw new Exception($"Null transitionalCorrelationProperty is not allowed. SagaDataType: {sagaData.GetType().FullName}.");
        }
        command.AddParameter("TransitionalCorrelationId", transitionalId);
    }

    public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaType = context.GetSagaType();
        return Update(sagaData, session, sagaType, GetConcurrency(context));
    }

    internal async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType, int concurrency)
    {
        var sqlSession = session.SqlPersistenceSession();
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType(), sagaType);

        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = sagaInfo.UpdateCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("SagaTypeVersion", sagaInfo.CurrentVersion);
            command.AddParameter("Data", sagaInfo.ToJson(sagaData));
            command.AddParameter("Concurrency", concurrency);
            AddTransitionalParameter(sagaData, sagaInfo, command);
            var affected = await command.ExecuteNonQueryAsync();
            if (affected != 1)
            {
                throw new Exception($"Optimistic concurrency violation when trying to save saga {sagaType.FullName} {sagaData.Id}. Expected version {concurrency}.");
            }
        }
    }


    public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await Get<TSagaData>(sagaId, session, sagaType);
        return SetConcurrency(result, context);
    }


    internal async Task<Concurrency<TSagaData>> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, Type sagaType)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
        var sqlSession = session.SqlPersistenceSession();
        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = sagaInfo.GetBySagaIdCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaId);
            return await GetSagaData<TSagaData>(command, sagaInfo);
        }
    }


    public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await Get<TSagaData>(propertyName, propertyValue, session, sagaType);
        return SetConcurrency(result, context);
    }

    static TSagaData SetConcurrency<TSagaData>(Concurrency<TSagaData> result, ContextBag context)
        where TSagaData : IContainSagaData
    {
        if (result.Data == null)
        {
            return default(TSagaData);
        }
        context.Set("NServiceBus.Persistence.Sql.Concurrency", result.Version);
        return result.Data;
    }

    static int GetConcurrency(ContextBag context)
    {
        int concurrency;
        if (!context.TryGet("NServiceBus.Persistence.Sql.Concurrency", out concurrency))
        {
            throw new Exception("Cannot save saga because optimistic concurrency version is missing in the context.");
        }
        return concurrency;
    }

    internal async Task<Concurrency<TSagaData>> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, Type sagaType)
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

        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = commandText;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("propertyValue", propertyValue.ToString());
            return await GetSagaData<TSagaData>(command, sagaInfo);
        }
    }


    static async Task<Concurrency<TSagaData>> GetSagaData<TSagaData>(DbCommand command, RuntimeSagaInfo sagaInfo)
        where TSagaData : IContainSagaData
    {
        // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
        using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess))
        {
            if (!await dataReader.ReadAsync())
            {
                return default(Concurrency<TSagaData>);
            }

            //TODO: MySql does not work for dataReader.GetFieldValueAsync<Guid>(0)
            var id = dataReader.GetGuid(0);
            var sagaTypeVersionString = await dataReader.GetFieldValueAsync<string>(1);
            var sagaTypeVersion = Version.Parse(sagaTypeVersionString);
            var concurrency = await dataReader.GetFieldValueAsync<int>(2);
            string originator;
            string originalMessageId;
            using (var textReader = dataReader.GetTextReader(3))
            {
                var metadata = Serializer.Deserialize<Dictionary<string, string>>(textReader);
                metadata.TryGetValue("Originator", out originator);
                metadata.TryGetValue("OriginalMessageId", out originalMessageId);
            }
            using (var textReader = dataReader.GetTextReader(4))
            {
                var sagaData = sagaInfo.FromString<TSagaData>(textReader, sagaTypeVersion);
                sagaData.Id = id;
                sagaData.Originator = originator;
                sagaData.OriginalMessageId = originalMessageId;
                return new Concurrency<TSagaData>(sagaData, concurrency);
            }
        }
    }


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

    internal struct Concurrency<TSagaData>
        where TSagaData : IContainSagaData
    {
        public TSagaData Data { get; }
        public int Version { get; }

        public Concurrency(TSagaData data, int version)
        {
            Data = data;
            Version = version;
        }
    }
}