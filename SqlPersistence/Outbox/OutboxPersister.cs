using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using IsolationLevel = System.Data.IsolationLevel;

class OutboxPersister : IOutboxStorage
{
    Func<DbConnection> connectionBuilder;
    string storeCommandText;
    string getCommandText;
    string setAsDispatchedCommandText;
    string cleanupCommandText;

    public OutboxPersister(
        Func<DbConnection> connectionBuilder,
        string tablePrefix)
    {
        this.connectionBuilder = connectionBuilder;
        var tableName = $@"{tablePrefix}OutboxData";
        storeCommandText = $@"
insert into {tableName}
(
    MessageId,
    Operations,
    PersistenceVersion
)
values
(
    @MessageId,
    @Operations,
    @PersistenceVersion
)";

        cleanupCommandText = $@"
delete from {tableName} where Dispatched = true And DispatchedAt < @Date";

        getCommandText = $@"
select
    Dispatched,
    Operations
from {tableName}
where MessageId = @MessageId";

        setAsDispatchedCommandText = $@"
update {tableName}
set
    Dispatched = 1,
    DispatchedAt = @DispatchedAt
where MessageId = @MessageId";
    }

    public async Task<OutboxTransaction> BeginTransaction(ContextBag context)
    {
        var connection = connectionBuilder();
        await connection.OpenAsync();
        var transaction = connection.BeginTransaction();
        return new SqlOutboxTransaction(transaction, connection);
    }


    public async Task SetAsDispatched(string messageId, ContextBag context)
    {
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = setAsDispatchedCommandText;
                command.AddParameter("MessageId", messageId);
                command.AddParameter("DispatchedAt", DateTime.UtcNow);
                command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
                await command.ExecuteNonQueryEx();
            }
        }
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync();
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
                            {
                                var transportOperations = Serializer.Deserialize<IEnumerable<SerializableOperation>>(textReader)
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
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            command.AddParameter("Operations", Serializer.Serialize(message.TransportOperations.ToSerializable()));
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress))
        using (var connection = connectionBuilder())
        {
            await connection.OpenAsync(cancellationToken);
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
}