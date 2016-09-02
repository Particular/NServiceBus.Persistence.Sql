using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using IsolationLevel = System.Data.IsolationLevel;

class OutboxPersister : IOutboxStorage
{
    string connectionString;
    string storeCommandText;
    string getCommandText;
    string setAsDispatchedCommandText;
    string cleanupCommandText;

    public OutboxPersister(string connectionString, string schema, string endpointName)
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

        cleanupCommandText = $@"
delete from [{schema}].[{endpointName}.OutboxData] where Dispatched = true And DispatchedAt < @Date";

        getCommandText = $@"
SELECT
    Operations
FROM [{schema}].[{endpointName}.OutboxData]
WHERE MessageId = @MessageId";

        setAsDispatchedCommandText = $@"
UPDATE [{schema}].[{endpointName}.OutboxData]
SET
    Dispatched = 1,
    DispatchedAt = @DispatchedAt
WHERE MessageId = @MessageId";
    }

    public async Task<OutboxTransaction> BeginTransaction(ContextBag context)
    {
        var sqlConnection = await SqlHelpers.New(connectionString);
        var sqlTransaction = sqlConnection.BeginTransaction();
        return new SqlOutboxTransaction(sqlTransaction, sqlConnection);
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
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            OutboxMessage result;
            using (var command = new SqlCommand(getCommandText, connection, transaction))
            {
                command.AddParameter("MessageId", messageId);
                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
                {
                    if (!await reader.ReadAsync())
                    {
                        return null;
                    }
                    result = new OutboxMessage(
                        messageId: messageId,
                        operations: OperationSerializer.FromString(reader.GetString(0)).ToArray());
                }
            }
            transaction.Commit();
            return result;
        }
    }

    public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
    {
        var sqlOutboxTransaction = (SqlOutboxTransaction) transaction;
        var sqlTransaction = sqlOutboxTransaction.SqlTransaction;
        var sqlConnection = sqlOutboxTransaction.SqlConnection;
        return Store(message, sqlTransaction, sqlConnection);
    }

    internal async Task Store(OutboxMessage message, SqlTransaction sqlTransaction, SqlConnection sqlConnection)
    {
        using (var command = new SqlCommand(storeCommandText, sqlConnection, sqlTransaction))
        {
            command.AddParameter("MessageId", message.MessageId);
            command.AddParameter("Operations", OperationSerializer.ToXml(message.TransportOperations));
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress))
        using (var connection = await SqlHelpers.New(connectionString))
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        using (var command = new SqlCommand(cleanupCommandText, connection, transaction))
        {
            command.AddParameter("Date", dateTime);
            await command.ExecuteNonQueryEx(cancellationToken);
        }
    }
}