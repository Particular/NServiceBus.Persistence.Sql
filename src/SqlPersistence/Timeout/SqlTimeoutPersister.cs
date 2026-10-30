using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Sagas;

// One row per timeout, scoped by saga id and timeout id, written through the session's own transaction.
sealed class SqlTimeoutPersister : ITimeoutPersister
{
    readonly SqlDialect sqlDialect;
    readonly string getCommand;
    readonly string requestCommand;
    readonly string rescheduleCommand;
    readonly string removeCommand;
    readonly string removeBySagaIdCommand;

    public SqlTimeoutPersister(SqlDialect sqlDialect, string tablePrefix)
    {
        this.sqlDialect = sqlDialect;
        var tableName = sqlDialect.GetTimeoutTableName(tablePrefix);
        getCommand = sqlDialect.GetTimeoutGetCommand(tableName);
        requestCommand = sqlDialect.GetTimeoutRequestCommand(tableName);
        rescheduleCommand = sqlDialect.GetTimeoutRescheduleCommand(tableName);
        removeCommand = sqlDialect.GetTimeoutRemoveCommand(tableName);
        removeBySagaIdCommand = sqlDialect.GetTimeoutRemoveBySagaIdCommand(tableName);
    }

    public async Task<IReadOnlyList<StoredSagaTimeout>> Get(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        using var command = CreateCommand(session, getCommand, sagaData.Id);
        var timeouts = new List<StoredSagaTimeout>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            timeouts.Add(Deserialize(reader.GetString(0)));
        }
        return timeouts;
    }

    public async Task Request(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        using var command = CreateCommand(session, requestCommand, sagaData.Id, timeout.TimeoutId);
        command.AddParameter("Data", Serialize(timeout));
        command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
        await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
    }

    public async Task Reschedule(IContainSagaData sagaData, StoredSagaTimeout timeout, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        using var command = CreateCommand(session, rescheduleCommand, sagaData.Id, timeout.TimeoutId);
        command.AddParameter("Data", Serialize(timeout));
        var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        if (affected != 1)
        {
            throw new Exception($"Cannot reschedule timeout '{timeout.TimeoutId}' of saga {sagaData.Id} because it does not exist.");
        }
    }

    public Task Cancel(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) =>
        Remove(sagaData, timeoutId, session, cancellationToken);

    public Task MarkFired(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default) =>
        Remove(sagaData, timeoutId, session, cancellationToken);

    async Task Remove(IContainSagaData sagaData, string timeoutId, ISynchronizedStorageSession session, CancellationToken cancellationToken)
    {
        using var command = CreateCommand(session, removeCommand, sagaData.Id, timeoutId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task RemoveAll(Guid sagaId, ISynchronizedStorageSession session, CancellationToken cancellationToken = default)
    {
        using var command = CreateCommand(session, removeBySagaIdCommand, sagaId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    CommandWrapper CreateCommand(ISynchronizedStorageSession session, string commandText, Guid sagaId, string timeoutId = null)
    {
        var sqlSession = session.SqlPersistenceSession();
        var command = sqlDialect.CreateCommand(sqlSession.Connection);
        command.CommandText = commandText;
        command.Transaction = sqlSession.Transaction;
        command.AddParameter("SagaId", sagaId);
        if (timeoutId != null)
        {
            command.AddParameter("TimeoutId", timeoutId);
        }
        return command;
    }

    internal static string Serialize(StoredSagaTimeout timeout) => Serializer.Serialize(timeout);

    internal static StoredSagaTimeout Deserialize(string json)
    {
        using var reader = new StringReader(json);
        return Serializer.Deserialize<StoredSagaTimeout>(reader);
    }
}
