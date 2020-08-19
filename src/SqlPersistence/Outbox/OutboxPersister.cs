using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using IsolationLevel = System.Data.IsolationLevel;
#pragma warning disable 618

class OutboxPersister : IOutboxStorage
{
    IConnectionManager connectionManager;
    SqlDialect sqlDialect;
    int cleanupBatchSize;
    OutboxCommands outboxCommands;
    Func<ISqlOutboxTransaction> outboxTransactionFactory;

    public OutboxPersister(IConnectionManager connectionManager, SqlDialect sqlDialect, OutboxCommands outboxCommands, Func<ISqlOutboxTransaction> outboxTransactionFactory, int cleanupBatchSize = 10000)
    {
        this.connectionManager = connectionManager;
        this.sqlDialect = sqlDialect;
        this.outboxCommands = outboxCommands;
        this.outboxTransactionFactory = outboxTransactionFactory;
        this.cleanupBatchSize = cleanupBatchSize;
    }

    public Task<OutboxTransaction> BeginTransaction(ContextBag context)
    {
        var transaction = outboxTransactionFactory();
        transaction.Prepare(context);
        return transaction.Begin(context);
    }

    public async Task SetAsDispatched(string messageId, ContextBag context)
    {
        using (var connection = await connectionManager.OpenConnection(context.GetIncomingMessage()).ConfigureAwait(false))
        using (var command = sqlDialect.CreateCommand(connection))
        {
            command.CommandText = outboxCommands.SetAsDispatched;
            command.AddParameter("MessageId", messageId);
            command.AddParameter("DispatchedAt", DateTime.UtcNow);
            await command.ExecuteNonQueryEx().ConfigureAwait(false);
        }
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = await connectionManager.OpenConnection(context.GetIncomingMessage()).ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            OutboxMessage result;
            using (var command = sqlDialect.CreateCommand(connection))
            {
                command.CommandText = outboxCommands.Get;
                command.Transaction = transaction;
                command.AddParameter("MessageId", messageId);
                // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed
                using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess).ConfigureAwait(false))
                {
                    if (!await dataReader.ReadAsync().ConfigureAwait(false))
                    {
                        return null;
                    }
                    var dispatched = await dataReader.GetBoolAsync(0).ConfigureAwait(false);
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
        var sqlOutboxTransaction = (ISqlOutboxTransaction)outboxTransaction;
        return sqlOutboxTransaction.Complete(message, context);
    }

    public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken)
    {
        using (var connection = await connectionManager.OpenNonContextualConnection().ConfigureAwait(false))
        {
            var continuePurging = true;
            while (continuePurging && !cancellationToken.IsCancellationRequested)
            {
                using (var command = sqlDialect.CreateCommand(connection))
                {
                    command.CommandText = outboxCommands.Cleanup;
                    command.AddParameter("DispatchedBefore", dateTime);
                    command.AddParameter("BatchSize", cleanupBatchSize);
                    var rowCount = await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
                    continuePurging = rowCount != 0;
                }
            }
        }
    }
}