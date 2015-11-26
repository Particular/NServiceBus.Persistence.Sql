using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using NServiceBus.Timeout.Core;
using System.Threading.Tasks;
using NServiceBus.Extensibility;

class TimeoutPersister : IPersistTimeouts, IQueryTimeouts
{
    string connectionString;
    string insertCommandText;
    string removeByIdCommandText;
    string removeBySagaIdCommandText;
    string selectByIdCommandText;
    string rangeComandText;
    string nextCommandText;

    public TimeoutPersister(string connectionString, string schema, string endpointName)
    {
        this.connectionString = connectionString;

        insertCommandText = $@"
INSERT INTO [{schema}].[{endpointName}.TimeoutData] 
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
DELETE FROM [{schema}].[{endpointName}.TimeoutData] 
OUTPUT deleted.SagaId
WHERE Id = @Id";

        removeBySagaIdCommandText = $@"
DELETE FROM [{schema}].[{endpointName}.TimeoutData] 
WHERE SagaId = @SagaId";

        selectByIdCommandText = $@"
SELECT 
    Destination, 
    SagaId, 
    State, 
    Time, 
    Headers
FROM [{schema}].[{endpointName}.TimeoutData] 
WHERE Id = @Id";

        rangeComandText = $@"
SELECT Id, Time 
FROM [{schema}].[{endpointName}.TimeoutData]
WHERE time BETWEEN @StartTime AND @EndTime";

        nextCommandText = $@"
SELECT TOP 1 Time FROM [{schema}].[{endpointName}.TimeoutData]
WHERE Time > @EndTime
ORDER BY TIME";
    }

    public async Task<TimeoutData> Peek(string timeoutId, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(selectByIdCommandText, connection))
        {
            command.AddParameter("Id", timeoutId);
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
            {
                if (!await reader.ReadAsync())
                {
                    return null;
                }
                return new TimeoutData
                {
                    Id = timeoutId,
                    Destination = reader.GetString(0),
                    SagaId = reader.GetGuid(1),
                    State = reader.GetSqlBinary(2).Value,
                    Time = reader.GetDateTime(3),
                    Headers = HeaderSerializer.FromString(reader.GetString(4)),
                };
            }
        }

    }

    public async Task Add(TimeoutData timeout, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(insertCommandText, connection))
        {
            var id = Guid.NewGuid();
            timeout.Id = id.ToString();
            command.AddParameter("Id", id);
            command.AddParameter("Destination", timeout.Destination);
            command.AddParameter("SagaId", timeout.SagaId);
            command.AddParameter("State", timeout.State);
            command.AddParameter("Time", timeout.Time);
            command.AddParameter("Headers", HeaderSerializer.ToXml(timeout.Headers));
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<bool> TryRemove(string timeoutId, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(removeByIdCommandText, connection))
        {
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
        using (var connection = await SqlHelpers.New(connectionString))
        {
            using (var command = new SqlCommand(rangeComandText, connection))
            {
                command.AddParameter("StartTime", startSlice);
                command.AddParameter("EndTime", now);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetGuid(0).ToString();
                        list.Add(new TimeoutsChunk.Timeout(id, reader.GetDateTime(1)));
                    }
                }
            }

            using (var command = new SqlCommand(nextCommandText, connection))
            {
                command.AddParameter("EndTime", now);
                var executeScalar = command.ExecuteScalar();
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
        return new TimeoutsChunk(list, nextTimeToRunQuery);
    }

    public async Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(removeBySagaIdCommandText, connection))
        {
            command.AddParameter("SagaId", sagaId);
            await command.ExecuteNonQueryEx();
        }
    }
    
}
