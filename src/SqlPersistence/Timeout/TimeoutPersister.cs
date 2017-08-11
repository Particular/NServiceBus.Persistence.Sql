using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using NServiceBus.Timeout.Core;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;
#pragma warning disable 618

class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
{
    Func<DbConnection> connectionBuilder;
    TimeoutCommands timeoutCommands;
    CommandBuilder commandBuilder;
    TimeSpan timeoutsCleanupExecutionInterval;
    DateTime lastTimeoutsCleanupExecution;
    DateTime oldestSupportedTimeout;

    public TimeoutPersister(Func<DbConnection> connectionBuilder, string tablePrefix, SqlDialect sqlDialect, TimeSpan timeoutsCleanupExecutionInterval)
    {
        this.connectionBuilder = connectionBuilder;
        this.timeoutsCleanupExecutionInterval = timeoutsCleanupExecutionInterval;
        timeoutCommands = TimeoutCommandBuilder.Build(sqlDialect, tablePrefix);
        commandBuilder = new CommandBuilder(sqlDialect);

        if (sqlDialect is SqlDialect.MsSqlServer)
        {
            oldestSupportedTimeout = SqlDateTime.MinValue.Value;
        }
        else if (sqlDialect is SqlDialect.Oracle || sqlDialect is SqlDialect.MySql)
        {
            oldestSupportedTimeout = new DateTime(1000, 1, 1);
        }
        else
        {
            throw new NotSupportedException("Not supported SQL dialect: " + sqlDialect.Name);
        }
    }

    public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = timeoutCommands.Peek;
            command.AddParameter("Id", timeoutId);
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
                    Time = dateTime,
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
        using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var command = commandBuilder.CreateCommand(connection))
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
        using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = timeoutCommands.RemoveById;
            command.AddParameter("Id", timeoutId);
            var rowsAffected = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return rowsAffected == 1;
        }
    }


    public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
    {
        var list = new List<TimeoutsChunk.Timeout>();
        var now = DateTime.UtcNow;

        //Every timeoutsCleanupExecutionInterval we extend the query window back in time to make
        //sure we will pick-up any missed timeouts which might exists due to TimeoutManager timeoute storeage race-condition
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
                        list.Add(new TimeoutsChunk.Timeout(id.ToString(), reader.GetDateTime(1)));
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
        return new TimeoutsChunk(list.ToArray(), nextTimeToRunQuery);
    }

    public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false))
        using (var command = commandBuilder.CreateCommand(connection))
        {
            command.CommandText = timeoutCommands.RemoveBySagaId;
            command.AddParameter("SagaId", sagaId);
            await command.ExecuteNonQueryEx().ConfigureAwait(false);
        }
    }

}