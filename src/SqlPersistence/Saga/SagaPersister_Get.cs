using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

partial class SagaPersister
{

    internal static async Task<TSagaData> GetByWhereClause<TSagaData>(string whereClause, SynchronizedStorageSession session, ContextBag context, ParameterAppender appendParameters, SagaInfoCache sagaInfoCache)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await GetByWhereClause<TSagaData>(whereClause, session, sagaType, appendParameters, sagaInfoCache);
        return SetConcurrency(result, context);
    }

    static Task<Concurrency<TSagaData>> GetByWhereClause<TSagaData>(string whereClause, SynchronizedStorageSession session, Type sagaType, ParameterAppender appendParameters, SagaInfoCache sagaInfoCache)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
        var commandText = $@"
{sagaInfo.SelectFromCommand}
where {whereClause}";
        return GetSagaData<TSagaData>(session, commandText, sagaInfo, appendParameters);
    }

    public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await Get<TSagaData>(propertyName, propertyValue, session, sagaType);
        return SetConcurrency(result, context);
    }

    internal Task<Concurrency<TSagaData>> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, Type sagaType)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);

        ValidatePropertyName<TSagaData>(propertyName, sagaInfo);
        var commandText = sagaInfo.GetByCorrelationPropertyCommand;
        return GetSagaData<TSagaData>(session, commandText, sagaInfo,
            appendParameters: (parameterBuilder, append) =>
            {
                var parameter = parameterBuilder();
                parameter.ParameterName = "propertyValue";
                parameter.Value = propertyValue;
                append(parameter);
            });
    }

    public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : IContainSagaData
    {
        var sagaType = context.GetSagaType();
        var result = await Get<TSagaData>(sagaId, session, sagaType);
        return SetConcurrency(result, context);
    }

    internal Task<Concurrency<TSagaData>> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, Type sagaType)
        where TSagaData : IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData), sagaType);
        return GetSagaData<TSagaData>(session, sagaInfo.GetBySagaIdCommand, sagaInfo,
            appendParameters: (parameterBuilder, append) =>
            {
                var parameter = parameterBuilder();
                parameter.ParameterName = "Id";
                parameter.Value = sagaId;
                append(parameter);
            });
    }

    static async Task<Concurrency<TSagaData>> GetSagaData<TSagaData>(SynchronizedStorageSession session, string commandText, RuntimeSagaInfo sagaInfo, ParameterAppender appendParameters)
        where TSagaData : IContainSagaData
    {
        var sqlSession = session.SqlPersistenceSession();

        using (var command = sqlSession.Connection.CreateCommand())
        {
            command.CommandText = commandText;
            command.Transaction = sqlSession.Transaction;
            appendParameters(command.CreateParameter, parameter => command.Parameters.Add(parameter));
            // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
            using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess))
            {
                if (!await dataReader.ReadAsync())
                {
                    return default(Concurrency<TSagaData>);
                }

                var id = await dataReader.GetGuidAsync(0);
                var sagaTypeVersionString = await dataReader.GetFieldValueAsync<string>(1);
                var sagaTypeVersion = Version.Parse(sagaTypeVersionString);
                var concurrency = await dataReader.GetFieldValueAsync<int>(2);
                string originator;
                string originalMessageId;
                ReadMetadata(dataReader, out originator, out originalMessageId);
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
    }

    static void ReadMetadata(DbDataReader dataReader, out string originator, out string originalMessageId)
    {
        using (var textReader = dataReader.GetTextReader(3))
        {
            var metadata = Serializer.Deserialize<Dictionary<string, string>>(textReader);
            metadata.TryGetValue("Originator", out originator);
            metadata.TryGetValue("OriginalMessageId", out originalMessageId);
        }
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

    static TSagaData SetConcurrency<TSagaData>(Concurrency<TSagaData> result, ContextBag context)
        where TSagaData : IContainSagaData
    {
        // ReSharper disable once CompareNonConstrainedGenericWithNull
        //TODO: remove when core adds a class constraint to TSagaData
        if (result.Data == null)
        {
            return default(TSagaData);
        }
        context.Set("NServiceBus.Persistence.Sql.Concurrency", result.Version);
        return result.Data;
    }
}