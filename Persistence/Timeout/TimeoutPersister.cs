using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using NServiceBus;
using NServiceBus.Timeout.Core;

class TimeoutPersister : IPersistTimeouts
{
    string connectionString;
    string schema;
    string endpointName;

    public TimeoutPersister(string connectionString,string schema, string endpointName)
    {
        this.connectionString = connectionString;
        this.schema = schema;
        this.endpointName = endpointName;
    }

    public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
    {
        var list = new List<Tuple<string, DateTime>>();
        var now = DateTime.UtcNow;
        using (var connection = SqlHelpers.New(connectionString))
        {
            var rangeComandText = string.Format(@"
SELECT Id, Time 
FROM [{0}].[{1}.TimeoutData]
WHERE time BETWEEN @StartTime AND @EndTime", schema, endpointName);
            using (var command = new SqlCommand(rangeComandText, connection))
            {
                command.AddParameter("StartTime", startSlice);
                command.AddParameter("EndTime", now);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Tuple<string, DateTime>(reader.GetGuid(0).ToString(), reader.GetDateTime(1)));
                    }
                }
            }
            var nextCommandText = string.Format(@"
SELECT TOP 1 Time FROM [{0}].[{1}.TimeoutData]
WHERE Time > @EndTime
ORDER BY TIME", schema, endpointName);

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
                    nextTimeToRunQuery = (DateTime) executeScalar;
                }
            }
        }
        return list;
    }

    public void Add(TimeoutData timeout)
    {
        var commandText = string.Format(@"
INSERT INTO [{0}].[{1}.TimeoutData] 
(
    Id, 
    Destination, 
    SagaId, 
    State, 
    Time, 
    Headers
) 
VALUES 
(
    @Id, 
    @Destination, 
    @SagaId, 
    @State, 
    @Time, 
    @Headers
)", schema, endpointName);
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(commandText, connection))
        {
            var id = Guid.NewGuid();
            timeout.Id = id.ToString();
            command.AddParameter("Id", id);
            command.AddParameter("Destination", timeout.Destination.ToString());
            command.AddParameter("SagaId", timeout.SagaId);
            command.AddParameter("State", timeout.State);
            command.AddParameter("Time", timeout.Time);
            command.AddParameter("Headers", HeaderSerializer.ToXml(timeout.Headers));
            command.ExecuteNonQuery();
        }
    }


    public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
    {
        var commandText = string.Format(@"
DELETE FROM [{0}].[{1}.TimeoutData] 
OUTPUT 
    deleted.Destination, 
    deleted.SagaId, 
    deleted.State, 
    deleted.Time, 
    deleted.Headers 
WHERE Id = @Id", schema, endpointName);
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(commandText, connection))
        {
            command.AddParameter("Id", timeoutId);
            using (var reader = command.ExecuteReader())
            {
                if (!reader.HasRows)
                {
                    timeoutData = null;
                    return false;
                }
                reader.Read();
                timeoutData = new TimeoutData
                {
                    Id = timeoutId,
                    Destination = Address.Parse(reader.GetString(0)),
                    SagaId = reader.GetGuid(1),
                    State = reader.GetSqlBinary(2).Value,
                    Time = reader.GetDateTime(3),
                    Headers = HeaderSerializer.FromString(reader.GetString(4)),
                };
            }
        }
        return true;
    }

    public void RemoveTimeoutBy(Guid sagaId)
    {
        var commandText = string.Format(@"
DELETE FROM [{0}].[{1}.TimeoutData] 
WHERE SagaId = @SagaId", schema, endpointName);
        using (var connection = SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(commandText, connection))
        {
            command.AddParameter("SagaId", sagaId);
            command.ExecuteNonQuery();
        }
    }
}
