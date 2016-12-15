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
            var metadata = new Dictionary<string,string>();
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
            //TODO:  validate non default for value types
            throw new Exception($"Null transitionalCorrelationProperty is not allowed. SagaDataType: {sagaData.GetType().FullName}.");
        }
        command.AddParameter("TransitionalCorrelationId", transitionalId);
    }

    public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaType = context.GetSagaType();
        return Update(sagaData, session, sagaType, GetSagaVersion(context));
    }

    internal async Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType, int sagaVersion)
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
            command.AddParameter("SagaVersion", sagaVersion);
            AddTransitionalParameter(sagaData, sagaInfo, command);
            var affected = await command.ExecuteNonQueryAsync();
            if (affected != 1)
            {
                throw new Exception($"Optimistic concurrency violation when trying to save saga {sagaType.FullName} {sagaData.Id}. Expected version {sagaVersion}.");
            }
        }
    }


    public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await Get<TSagaData>(sagaId, session, sagaType);
        return SetSagaVersion(result, context);
    }


    internal async Task<SagaVersion<TSagaData>> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, Type sagaType)
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
        return SetSagaVersion(result, context);
    }

    static TSagaData SetSagaVersion<TSagaData>(SagaVersion<TSagaData> result, ContextBag context) where TSagaData : IContainSagaData
    {
        if (result.Data == null)
        {
            return default(TSagaData);
        }
        context.Set("NServiceBus.Persistence.Sql.SagaVersion", result.Version);
        return result.Data;
    }

    static int GetSagaVersion(ContextBag context)
    {
        int sagaVersion;
        if (!context.TryGet("NServiceBus.Persistence.Sql.SagaVersion", out sagaVersion))
        {
            throw new Exception("Cannot save saga because optimistic concurrency version is missing in the context.");
        }
        return sagaVersion;
    }

    internal async Task<SagaVersion<TSagaData>> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, Type sagaType) 
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


    static async Task<SagaVersion<TSagaData>> GetSagaData<TSagaData>(DbCommand command, RuntimeSagaInfo sagaInfo)
        where TSagaData : IContainSagaData
    {
        using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
        {
            if (!await dataReader.ReadAsync())
            {
                return default(SagaVersion<TSagaData>);
            }
            var id = dataReader.GetGuid(0);
            string originator;
            string originalMessageId;
            using (var textReader = dataReader.GetTextReader(1))
            {
                var metadata = Serializer.Deserialize<Dictionary<string,string>>(textReader);
                metadata.TryGetValue("Originator", out originator);
                metadata.TryGetValue("OriginalMessageId", out originalMessageId);
            }
            var sagaTypeVersion = Version.Parse(dataReader.GetString(3));
            var sagaVersion = dataReader.GetInt32(4);
            using (var textReader = dataReader.GetTextReader(2))
            {
                var sagaData = sagaInfo.FromString<TSagaData>(textReader, sagaTypeVersion);
                sagaData.Id = id;
                sagaData.Originator = originator;
                sagaData.OriginalMessageId = originalMessageId;
                return new SagaVersion<TSagaData>(sagaData, sagaVersion);
            }
        }
    }


    public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
    {
        var sagaType = context.GetSagaType();
        return Complete(sagaData, session, sagaType, GetSagaVersion(context));
    }

    internal async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, Type sagaType, int sagaVersion)
    {
        var sagaInfo = sagaInfoCache.GetInfo(sagaData.GetType(), sagaType);
        var sqlSession = session.SqlPersistenceSession();

        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = sagaInfo.CompleteCommand;
            command.Transaction = sqlSession.Transaction;
            command.AddParameter("Id", sagaData.Id);
            command.AddParameter("SagaVersion", sagaVersion);
            await command.ExecuteNonQueryAsync();
        }
    }

    internal struct SagaVersion<TSagaData>
        where TSagaData : IContainSagaData
    {
        public TSagaData Data { get; }
        public int Version { get; }

        public SagaVersion(TSagaData data, int version)
        {
            Data = data;
            Version = version;
        }
    }
}