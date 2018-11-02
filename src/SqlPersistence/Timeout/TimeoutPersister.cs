using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NServiceBus.Timeout.Core;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;
#pragma warning disable 618

class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
{
    Func<ContextBag, DbConnection> connectionBuilder;
    SqlDialect sqlDialect;
    TimeoutCommands timeoutCommands;
    TimeSpan timeoutsCleanupExecutionInterval;
    Func<DateTime> utcNow;
    DateTime lastTimeoutsCleanupExecution;
    DateTime oldestSupportedTimeout;

    public TimeoutPersister(Func<ContextBag, DbConnection> connectionBuilder, string tablePrefix, SqlDialect sqlDialect, TimeSpan timeoutsCleanupExecutionInterval, Func<DateTime> utcNow)
    {
        this.connectionBuilder = connectionBuilder;
        this.sqlDialect = sqlDialect;
        this.timeoutsCleanupExecutionInterval = timeoutsCleanupExecutionInterval;
        this.utcNow = utcNow;
        timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, tablePrefix);
        oldestSupportedTimeout = sqlDialect.OldestSupportedTimeout;
    }

    public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
    {
        var guid = sqlDialect.ConvertTimeoutId(timeoutId);
        using (var connection = await connectionBuilder.OpenConnection(context).ConfigureAwait(false))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = timeoutCommands.Peek;
            command.AddParameter("Id", guid);
            // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync().ConfigureAwait(false))
                {
                    return null;
                }

                var destination = await reader.GetFieldValueAsync<string>(0).ConfigureAwait(false);
                var sagaId = await reader.GetGuidAsync(1).ConfigureAwait(false);
                var value = await reader.GetFieldValueAsync<byte[]>(2).ConfigureAwait(false);
                var dateTime = await reader.GetFieldValueAsync<DateTime>(3).ConfigureAwait(false);
                var headers = ReadHeaders(reader);

                return new TimeoutData
                {
                    Id = timeoutId,
                    Destination = destination,
                    SagaId = sagaId,
                    State = value,
                    Time = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
                    Headers = headers,
                };
            }
        }
    }

    static Dictionary<string, string> ReadHeaders(DbDataReader reader)
    {
        using (var stream = reader.GetTextReader(4))
        {
            return Serializer.Deserialize<Dictionary<string, string>>(stream);
        }
    }

    public async Task Add(TimeoutData timeout, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection(context).ConfigureAwait(false))
        using (var command = sqlDialect.CreateCommand(connection))
        {
            command.CommandText = timeoutCommands.Add;
            var id = SequentialGuid.Next();
            timeout.Id = id.ToString();
            command.AddParameter("Id", id);
            command.AddParameter("Destination", timeout.Destination);
            command.AddParameter("SagaId", timeout.SagaId);
            command.AddParameter("State", timeout.State);
            command.AddParameter("Time", timeout.Time);
            command.AddParameter("Headers", Serializer.Serialize(timeout.Headers));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx().ConfigureAwait(false);
        }
    }


    public async Task<bool> TryRemove(string timeoutId, ContextBag context)
    {
        var guid = sqlDialect.ConvertTimeoutId(timeoutId);
        using (var connection = await connectionBuilder.OpenConnection(context).ConfigureAwait(false))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = timeoutCommands.RemoveById;
            command.AddParameter("Id", guid);
            var rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return rowsAffected == 1;
        }
    }

    public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
    {
        var list = new List<TimeoutsChunk.Timeout>();
        var now = utcNow();

        //Every timeoutsCleanupExecutionInterval we extend the query window back in time to make
        //sure we will pick-up any missed timeouts which might exists due to TimeoutManager timeout storage race-condition
        if (lastTimeoutsCleanupExecution.Add(timeoutsCleanupExecutionInterval) < now)
        {
            lastTimeoutsCleanupExecution = now;
            startSlice = oldestSupportedTimeout;
        }

        DateTime nextTimeToRunQuery;
        using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.Range;
                command.AddParameter("StartTime", startSlice);
                command.AddParameter("EndTime", now);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var id = await reader.GetGuidAsync(0).ConfigureAwait(false);
                        list.Add(new TimeoutsChunk.Timeout(id.ToString(), DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc)));
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.Next;
                command.AddParameter("EndTime", now);
                var executeScalar = await command.ExecuteScalarAsync().ConfigureAwait(false);
                if (executeScalar == null)
                {
                    nextTimeToRunQuery = now.AddMinutes(10);
                }
                else
                {
                    nextTimeToRunQuery = (DateTime)executeScalar;
                }
            }
        }
        return new TimeoutsChunk(list.ToArray(), DateTime.SpecifyKind(nextTimeToRunQuery, DateTimeKind.Utc));
    }

    public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection(context).ConfigureAwait(false))
        using (var command = sqlDialect.CreateCommand(connection))
        {
            command.CommandText = timeoutCommands.RemoveBySagaId;
            command.AddParameter("SagaId", sagaId);
            await command.ExecuteNonQueryEx().ConfigureAwait(false);
        }
    }
}