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
using NServiceBus.Persistence.Sql;
using IsolationLevel = System.Data.IsolationLevel;

class OutboxPersister : IOutboxStorage
{
    Func<DbConnection> connectionBuilder;
    OutboxCommands outboxCommands;

    public OutboxPersister(
        SqlVariant sqlVariant,
        Func<DbConnection> connectionBuilder,
        string tablePrefix)
    {
        this.connectionBuilder = connectionBuilder;
        outboxCommands = OutboxCommandBuilder.Build(sqlVariant, tablePrefix);
    }

    public async Task<OutboxTransaction> BeginTransaction(ContextBag context)
    {
        var connection = await connectionBuilder.OpenConnection();
        var transaction = connection.BeginTransaction();
        return new SqlOutboxTransaction(transaction, connection);
    }



    public async Task SetAsDispatched(string messageId, ContextBag context)
    {
        using (var connection = await connectionBuilder.OpenConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = outboxCommands.SetAsDispatched;
            command.AddParameter("MessageId", messageId);
            command.AddParameter("DispatchedAt", DateTime.UtcNow);
            command.AddParameter("PersistenceVersion", StaticVersions.PersistenceVersion);
            await command.ExecuteNonQueryEx();
        }
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = await connectionBuilder.OpenConnection())
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            OutboxMessage result;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = outboxCommands.Get;
                command.Transaction = transaction;
                command.AddParameter("MessageId", messageId);
                // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
                using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess))
                {
                    if (!await dataReader.ReadAsync())
                    {
                        return null;
                    }
                    var dispatched = await dataReader.GetBoolAsync(0);
                    using (var textReader = dataReader.GetTextReader(1))
                    {
                        if (dispatched)
                        {
                            result = new OutboxMessage(messageId, new TransportOperation[0]);
                        }
                        else
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
            command.CommandText = outboxCommands.Store;
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
        using (var connection = await connectionBuilder.OpenConnection())
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = outboxCommands.Cleanup;
            command.Transaction = transaction;
            command.AddParameter("Date", dateTime);
            await command.ExecuteNonQueryEx(cancellationToken);
        }
    }
}