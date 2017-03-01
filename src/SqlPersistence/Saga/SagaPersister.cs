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


    public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        return Get<TSagaData>(sagaId, session, sagaType, context);
    }

    public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        return Get<TSagaData>(propertyName, propertyValue, session, sagaType, context);
    }


    internal Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, Type sagaType, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
        var sqlSession = session.GetSqlStorageSession();
        return GetSagaData<TSagaData>(sagaInfo, context, sqlSession, command =>
        {
            command.CommandText = sagaInfo.GetBySagaIdCommand;
            command.AddParameter("Id", sagaId);
        });
    }

    internal Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, Type sagaType, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
        ValidatePropertyName<TSagaData>(propertyName, sagaInfo);
        var sqlSession = session.GetSqlStorageSession();
        return GetSagaData<TSagaData>(sagaInfo, context, sqlSession,
            command =>
            {
                command.CommandText = sagaInfo.GetByCorrelationPropertyCommand;
                command.AddParameter("propertyValue", propertyValue.ToString());
            });
    }


    internal static async Task<TSagaData> GetSagaData<TSagaData>(RuntimeSagaInfo sagaInfo, ContextBag context, StorageSession sqlSession, Action<DbCommand> manipulateCommand)
        where TSagaData : IContainSagaData
    {
        using (var command = sqlSession.Connection.CreateCommand())
        {
            manipulateCommand(command);
            command.Transaction = sqlSession.Transaction;
            // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
            using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess))
            {
                if (!await dataReader.ReadAsync())
                {
                    return default(TSagaData);
                }

                var id = await dataReader.GetGuidAsync(0);
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
                    SetConcurrency(context, concurrency);
                    return sagaData;
                }
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

    static void SetConcurrency(ContextBag context, int concurrency)
    {
        context.Set("NServiceBus.Persistence.Sql.Concurrency", concurrency);
    }

    internal static int GetConcurrency(ContextBag context)
    {
        int concurrency;
        if (!context.TryGet("NServiceBus.Persistence.Sql.Concurrency", out concurrency))
        {
            throw new Exception("Cannot save saga because optimistic concurrency version is missing in the context.");
        }
        return concurrency;
    }

    static void ValidatePropertyName<TSagaData>(string propertyName, RuntimeSagaInfo sagaInfo) where TSagaData : IContainSagaData
    {
        if (!sagaInfo.HasCorrelationProperty)
        {
            throw new Exception($"Cannot retrieve a {typeof(TSagaData).FullName} using property \'{propertyName}\'. The saga has no correlation property.");
        }
        if (propertyName != sagaInfo.CorrelationProperty)
        {
            throw new Exception($"Cannot retrieve a {typeof(TSagaData).FullName} using property \'{propertyName}\'. Can only be retrieve using the correlation property '{sagaInfo.CorrelationProperty}'");
        }
    }

}