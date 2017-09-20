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
        where TSagaData : class, IContainSagaData
    {
        var result = await GetByWhereClause<TSagaData>(whereClause, session, appendParameters, sagaInfoCache).ConfigureAwait(false);
        return SetConcurrency(result, context);
    }

    static Task<Concurrency<TSagaData>> GetByWhereClause<TSagaData>(string whereClause, SynchronizedStorageSession session, ParameterAppender appendParameters, SagaInfoCache sagaInfoCache)
        where TSagaData : class, IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData));
        var commandText = $@"
{sagaInfo.SelectFromCommand}
where {whereClause}";
        return GetSagaData<TSagaData>(session, commandText, sagaInfo, appendParameters);
    }

    public async Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : class, IContainSagaData
    {
        var result = await Get<TSagaData>(propertyName, propertyValue, session).ConfigureAwait(false);
        return SetConcurrency(result, context);
    }

    internal Task<Concurrency<TSagaData>> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session)
        where TSagaData : class, IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData));

        ValidatePropertyName<TSagaData>(propertyName, sagaInfo);
        var commandText = sagaInfo.GetByCorrelationPropertyCommand;
        return GetSagaData<TSagaData>(session, commandText, sagaInfo,
            appendParameters: (parameterBuilder, append) =>
            {
                var parameter = parameterBuilder();
                sqlDialect.FillParameter(parameter, "propertyValue", propertyValue);
                append(parameter);
            });
    }

    public async Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
        where TSagaData : class, IContainSagaData
    {
        var result = await Get<TSagaData>(sagaId, session).ConfigureAwait(false);
        return SetConcurrency(result, context);
    }

    internal Task<Concurrency<TSagaData>> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session)
        where TSagaData : class, IContainSagaData
    {
        var sagaInfo = sagaInfoCache.GetInfo(typeof(TSagaData));
        return GetSagaData<TSagaData>(session, sagaInfo.GetBySagaIdCommand, sagaInfo,
            appendParameters: (parameterBuilder, append) =>
            {
                var parameter = parameterBuilder();
                sqlDialect.FillParameter(parameter, "Id", sagaId);
                append(parameter);
            });
    }

    static async Task<Concurrency<TSagaData>> GetSagaData<TSagaData>(SynchronizedStorageSession session, string commandText, RuntimeSagaInfo sagaInfo, ParameterAppender appendParameters)
        where TSagaData : class, IContainSagaData
    {
        var sqlSession = session.SqlPersistenceSession();

        using (var command = sagaInfo.CreateCommand(sqlSession.Connection))
        {
            command.CommandText = commandText;
            command.Transaction = sqlSession.Transaction;
            var dbCommand = command.InnerCommand;
            appendParameters(dbCommand.CreateParameter, parameter => dbCommand.Parameters.Add(parameter));
            // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
            using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess).ConfigureAwait(false))
            {
                if (!await dataReader.ReadAsync().ConfigureAwait(false))
                {
                    return default;
                }

                var id = await dataReader.GetGuidAsync(0).ConfigureAwait(false);
                var sagaTypeVersionString = await dataReader.GetFieldValueAsync<string>(1).ConfigureAwait(false);
                var sagaTypeVersion = Version.Parse(sagaTypeVersionString);
                var concurrency = await dataReader.GetFieldValueAsync<int>(2).ConfigureAwait(false);
                ReadMetadata(dataReader, out var originator, out var originalMessageId);
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

    static void ValidatePropertyName<TSagaData>(string propertyName, RuntimeSagaInfo sagaInfo)
        where TSagaData : IContainSagaData
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
        where TSagaData : class, IContainSagaData
    {
        if (result.Data == null)
        {
            return null;
        }
        context.Set("NServiceBus.Persistence.Sql.Concurrency", result.Version);
        return result.Data;
    }
}