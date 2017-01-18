using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NServiceBus.Timeout.Core;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.Sql;

class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
{
    Func<DbConnection> connectionBuilder;
    TimeoutCommands timeoutCommands;

    public TimeoutPersister(
        Func<DbConnection> connectionBuilder,
        string tablePrefix, SqlVariant sqlVariant)
    {
        this.connectionBuilder = connectionBuilder;
        timeoutCommands = TimeoutCommandBuilder.Build(sqlVariant, tablePrefix);
    }

    public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
    {
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.Peek;
                command.AddParameter("Id", timeoutId);
                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
                {
                    if (!await reader.ReadAsync())
                    {
                        return null;
                    }

                    var headers = ReadHeaders(reader);
                    return new TimeoutData
                    {
                        Id = timeoutId,
                        Destination = reader.GetString(0),
                        SagaId = reader.GetGuid(1),
                        State = (byte[]) reader.GetValue(2),
                        Time = reader.GetDateTime(3),
                        Headers = headers,
                    };
                }
            }
        }
    }

    Dictionary<string, string> ReadHeaders(DbDataReader reader)
    {
        using (var textReader = reader.GetTextReader(4))
        {
            return Serializer.Deserialize<Dictionary<string, string>>(textReader);
        }
    }

    public async Task Add(TimeoutData timeout, ContextBag context)
    {
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.Add;
                var id = Guid.NewGuid();
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
    }

    public async Task<bool> TryRemove(string timeoutId, ContextBag context)
    {
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.RemoveById;
                command.AddParameter("Id", timeoutId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {
                        return false;
                    }
                    await reader.ReadAsync();
                    var value = reader.GetValue(0);
                    return value != DBNull.Value;
                }
            }
        }
    }

    public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
    {
        var list = new List<TimeoutsChunk.Timeout>();
        var now = DateTime.UtcNow;
        DateTime nextTimeToRunQuery;
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.Range;
                command.AddParameter("StartTime", startSlice);
                command.AddParameter("EndTime", now);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetGuid(0).ToString();
                        list.Add(new TimeoutsChunk.Timeout(id, reader.GetDateTime(1)));
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
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = timeoutCommands.RemoveBySagaId;
                command.AddParameter("SagaId", sagaId);
                await command.ExecuteNonQueryEx();
            }
        }
    }

}