using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NServiceBus.Timeout.Core;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;
#pragma warning disable 618

class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
{
    Func<DbConnection> connectionBuilder;
    TimeoutCommands timeoutCommands;
    CommandBuilder commandBuilder;

    public TimeoutPersister(Func<DbConnection> connectionBuilder, string tablePrefix, SqlVariant sqlVariant, string schema)
    {
        this.connectionBuilder = connectionBuilder;
        timeoutCommands = TimeoutCommandBuilder.Build(sqlVariant, tablePrefix, schema);
        commandBuilder = new CommandBuilder(sqlVariant);
    }

    public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = timeoutCommands.Peek;
            command.AddParameter("Id", timeoutId);
            // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess))
            {
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                var destination = await reader.GetFieldValueAsync<string>(0);
                var sagaId = await reader.GetGuidAsync(1);
                var value = await reader.GetFieldValueAsync<byte[]>(2);
                var dateTime = await reader.GetFieldValueAsync<DateTime>(3);
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
        using (var connection = await connectionBuilder.OpenConnection())
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
            await command.ExecuteNonQueryEx();
        }
    }


    public async Task<bool> TryRemove(string timeoutId, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = timeoutCommands.RemoveById;
            command.AddParameter("Id", timeoutId);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected == 1;
        }
    }


    public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
    {
        var list = new List<TimeoutsChunk.Timeout>();
        var now = DateTime.UtcNow;
        DateTime nextTimeToRunQuery;
        using (var connection = await connectionBuilder.OpenConnection())
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.Range;
                command.AddParameter("StartTime", startSlice);
                command.AddParameter("EndTime", now);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = await reader.GetGuidAsync(0);
                        list.Add(new TimeoutsChunk.Timeout(id.ToString(), reader.GetDateTime(1)));
                    }
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.Next;
                command.AddParameter("EndTime", now);
                var executeScalar = await command.ExecuteScalarAsync();
                if (executeScalar == null)
                {
                    nextTimeToRunQuery = now.AddMinutes(10);
                }
                else
                {
                    nextTimeToRunQuery = (DateTime) executeScalar;
                }
            }
        }
        return new TimeoutsChunk(list.ToArray(), nextTimeToRunQuery);
    }

    public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        using (var command = commandBuilder.CreateCommand(connection))
        {
            command.CommandText = timeoutCommands.RemoveBySagaId;
            command.AddParameter("SagaId", sagaId);
            await command.ExecuteNonQueryEx();
        }
    }

}