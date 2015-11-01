using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

class OutboxStorage : IOutboxStorage
{
    string connectionString;
    string storeCommandText;
    string getCommandText;
    string setAsDispatchedCommandText;

    public OutboxStorage(string connectionString, string schema, string endpointName)
    {
        this.connectionString = connectionString;
        storeCommandText = $@"
INSERT INTO [{schema}].[{endpointName}.OutboxData] 
(
    MessageId, 
    Operations
) 
VALUES 
(
    @MessageId, 
    @Operations
)";

        getCommandText = $@"
SELECT 
    Operations
FROM [{schema}].[{endpointName}.OutboxData] 
WHERE MessageId = @MessageId";

        setAsDispatchedCommandText = $@"
UPDATE [{schema}].[{endpointName}.OutboxData]
SET
    Dispatched = 1, 
    DispatchedAt = @DispatchedAt, 
WHERE MessageId = @MessageId";
    }

    public Task<OutboxTransaction> BeginTransaction(ContextBag context)
    {
        //TODO: resolve sql transaction
        var sqlOutboxTransaction = new SqlOutboxTransaction(null);

        return Task.FromResult<OutboxTransaction>(sqlOutboxTransaction);
    }


    public async Task SetAsDispatched(string messageId, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(setAsDispatchedCommandText, connection))
        {
            command.AddParameter("MessageId", messageId);
            command.AddParameter("DispatchedAt", DateTime.UtcNow);
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context)
    {
        using (var connection = await SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(getCommandText, connection))
        {
            command.AddParameter("MessageId", messageId);
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
            {
                if (!await reader.ReadAsync())
                {
                    return null;
                }
                return new OutboxMessage(
                    messageId: messageId, 
                    operations: OperationSerializer.FromString(reader.GetString(0)).ToList());
            }
        }
    }

    public async Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
    {
        var sqlTransaction = ((SqlOutboxTransaction)transaction).SqlTransaction;
        using (var connection = await SqlHelpers.New(connectionString))
        using (var command = new SqlCommand(storeCommandText, connection, sqlTransaction))
        {
            command.AddParameter("MessageId", message.MessageId);
            command.AddParameter("Operations", OperationSerializer.ToXml(message.TransportOperations));
            await command.ExecuteNonQueryEx();
        }
    }
}