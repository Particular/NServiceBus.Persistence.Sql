using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Newtonsoft.Json;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using IsolationLevel = System.Data.IsolationLevel;

class OutboxPersister : IOutboxStorage
{
    Func<Task<DbConnection>> connectionBuilder;
    JsonSerializer jsonSerializer;
    Func<TextReader, JsonReader> readerCreator;
    Func<StringBuilder, JsonWriter> writerCreator;
    string storeCommandText;
    string getCommandText;
    string setAsDispatchedCommandText;
    string cleanupCommandText;

    public OutboxPersister(
        Func<Task<DbConnection>> connectionBuilder, 
        string schema, 
        string endpointName,
        JsonSerializer jsonSerializer,
        Func<TextReader, JsonReader> readerCreator,
        Func<StringBuilder, JsonWriter> writerCreator)
    {
        this.connectionBuilder = connectionBuilder;
        this.jsonSerializer = jsonSerializer;
        this.readerCreator = readerCreator;
        this.writerCreator = writerCreator;
        storeCommandText = $@"
INSERT INTO [{schema}].[{endpointName}OutboxData]
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
delete from [{schema}].[{endpointName}OutboxData] where Dispatched = true And DispatchedAt < @Date";

        getCommandText = $@"
SELECT
    Dispatched,
    Operations
FROM [{schema}].[{endpointName}OutboxData]
WHERE MessageId = @MessageId";

        setAsDispatchedCommandText = $@"
UPDATE [{schema}].[{endpointName}OutboxData]
SET
    Dispatched = 1,
    DispatchedAt = @DispatchedAt
WHERE MessageId = @MessageId";
    }

    public async Task<OutboxTransaction> BeginTransaction(ContextBag context)
    {
        var connection = await connectionBuilder();
        var transaction = connection.BeginTransaction();
        return new SqlOutboxTransaction(transaction, connection);
    }


    public async Task SetAsDispatched(string messageId, ContextBag context)
    {
        using (var connection = await connectionBuilder())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = setAsDispatchedCommandText;
            command.AddParameter("MessageId", messageId);
            command.AddParameter("DispatchedAt", DateTime.UtcNow);
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = await connectionBuilder())
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            OutboxMessage result;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = getCommandText;
                command.Transaction = transaction;
                command.AddParameter("MessageId", messageId);
                using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
                {
                    if (!await dataReader.ReadAsync())
                    {
                        return null;
                    }
                    var dispatched = dataReader.GetBoolean(0);
                    if (dispatched)
                    {
                        result = new OutboxMessage(messageId, new TransportOperation[0]);
                    }
                    else
                    {
                        using (var textReader = dataReader.GetTextReader(1))
                        using (var jsonReader = readerCreator(textReader))
                        {
                            var transportOperations = jsonSerializer.Deserialize<IEnumerable<SerializableOperation>>(jsonReader)
                                .FromSerializable()
                                .ToArray();
                            result = new OutboxMessage(messageId, transportOperations);
                        }
                    }
                }
            }
            transaction.Commit();
            return result;
        }
    }

    public Task Store(OutboxMessage message, OutboxTransaction outboxTransaction, ContextBag context)
    {
        var sqlOutboxTransaction = (SqlOutboxTransaction) outboxTransaction;
        var transaction = sqlOutboxTransaction.Transaction;
        var connection = sqlOutboxTransaction.Connection;
        return Store(message, transaction, connection);
    }

    internal async Task Store(OutboxMessage message, DbTransaction transaction, DbConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = storeCommandText;
            command.Transaction = transaction;
            command.AddParameter("MessageId", message.MessageId);
            command.AddParameter("Operations", OperationsToString(message.TransportOperations));
            await command.ExecuteNonQueryEx();
        }
    }

    string OperationsToString(TransportOperation[] operations)
    {
        var stringBuilder = new StringBuilder();
        using (var jsonWriter = writerCreator(stringBuilder))
        {
            jsonSerializer.Serialize(jsonWriter, operations.ToSerializable());
        }
        return stringBuilder.ToString();
    }

    public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress))
        using (var connection = await connectionBuilder())
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = cleanupCommandText;
            command.Transaction = transaction;
            command.AddParameter("Date", dateTime);
            await command.ExecuteNonQueryEx(cancellationToken);
        }
    }
}