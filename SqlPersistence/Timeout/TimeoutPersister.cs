using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using NServiceBus.Timeout.Core;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NServiceBus.Extensibility;

class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
{
    Func<Task<DbConnection>> connectionBuilder;
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<StringBuilder, JsonWriter> writerCreator;
    string insertCommandText;
    string removeByIdCommandText;
    string removeBySagaIdCommandText;
    string selectByIdCommandText;
    string rangeComandText;
    string nextCommandText;

    public TimeoutPersister(Func<Task<DbConnection>> connectionBuilder, string schema, string endpointName,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<StringBuilder, JsonWriter> writerCreator)
    {
        this.connectionBuilder = connectionBuilder;
        this.jsonSerializer = jsonSerializer;
        this.readerCreator = readerCreator;
        this.writerCreator = writerCreator;

        insertCommandText = $@"
INSERT INTO [{schema}].[{endpointName}TimeoutData]
(
    Id,
    Destination,
    SagaId,
    State,
    Time,
    Headers,
    PersistenceVersion
)
VALUES
(
    @Id,
    @Destination,
    @SagaId,
    @State,
    @Time,
    @Headers,
    @PersistenceVersion
)";

        removeByIdCommandText = $@"
DELETE FROM [{schema}].[{endpointName}TimeoutData]
OUTPUT deleted.SagaId
WHERE Id = @Id";

        removeBySagaIdCommandText = $@"
DELETE FROM [{schema}].[{endpointName}TimeoutData]
WHERE SagaId = @SagaId";

        selectByIdCommandText = $@"
SELECT
    Destination,
    SagaId,
    State,
    Time,
    Headers
FROM [{schema}].[{endpointName}TimeoutData]
WHERE Id = @Id";

        rangeComandText = $@"
SELECT Id, Time
FROM [{schema}].[{endpointName}TimeoutData]
WHERE time BETWEEN @StartTime AND @EndTime";

        nextCommandText = $@"
SELECT TOP 1 Time FROM [{schema}].[{endpointName}TimeoutData]
WHERE Time > @EndTime
ORDER BY TIME";
    }

    public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
    {
        using (var connection = await connectionBuilder())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = selectByIdCommandText;
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

    Dictionary<string, string> ReadHeaders(DbDataReader reader)
    {
        using (var textReader = reader.GetTextReader(4))
        using (var jsonReader = readerCreator(textReader))
        {
            return jsonSerializer.Deserialize<Dictionary<string, string>>(jsonReader);
        }
    }

    public async Task Add(TimeoutData timeout, ContextBag context)
    {
        using (var connection = await connectionBuilder())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = insertCommandText;
            var id = Guid.NewGuid();
            timeout.Id = id.ToString();
            command.AddParameter("Id", id);
            command.AddParameter("Destination", timeout.Destination);
            command.AddParameter("SagaId", timeout.SagaId);
            command.AddParameter("State", timeout.State);
            command.AddParameter("Time", timeout.Time);
            command.AddParameter("Headers", HeadersToString(timeout.Headers));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx();
        }
    }

    string HeadersToString(Dictionary<string, string> headers)
    {
        var stringBuilder = new StringBuilder();
        using (var jsonWriter = writerCreator(stringBuilder))
        {
            jsonSerializer.Serialize(jsonWriter, headers);
        }
        return stringBuilder.ToString();
    }

    public async Task<bool> TryRemove(string timeoutId, ContextBag context)
    {
        using (var connection = await connectionBuilder())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = removeByIdCommandText;
            command.AddParameter("Id", timeoutId);
            using (var reader = await command.ExecuteReaderAsync())
            {
                if (!reader.HasRows)
                {
                    return false;
                }
            }
        }
        return true;
    }
    public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
    {
        var list = new List<TimeoutsChunk.Timeout>();
        var now = DateTime.UtcNow;
        DateTime nextTimeToRunQuery;
        using (var connection = await connectionBuilder())
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = rangeComandText;
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
                command.CommandText = nextCommandText;
                command.AddParameter("EndTime", now);
                var executeScalar = await command.ExecuteScalarAsync();
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
        using (var connection = await connectionBuilder())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = removeBySagaIdCommandText;
            command.AddParameter("SagaId", sagaId);
            await command.ExecuteNonQueryEx();
        }
    }

}
