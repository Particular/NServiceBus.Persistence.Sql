using System;
using System.Data;
using System.Data.SqlClient;
using NServiceBus.Timeout.Core;
using System.Threading.Tasks;
using NServiceBus.Extensibility;

class TimeoutPersister : IPersistTimeouts
{
    string connectionString;
    string insertCommandText;
    string removeByIdCommandText;
    string removeBySagaIdCommandText;
    string selectByIdCommandText;

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
            command.AddParameter("PersistenceVersion", StaticVersions.PeristenceVersion);
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
