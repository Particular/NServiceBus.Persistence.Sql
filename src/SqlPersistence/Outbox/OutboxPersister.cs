﻿using System;
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

class OutboxPersister : IOutboxStorage
{
    readonly IConnectionManager connectionManager;
    readonly SqlDialect sqlDialect;
    readonly OutboxCommands outboxCommands;
    readonly Func<ISqlOutboxTransaction> outboxTransactionFactory;

    public OutboxPersister(IConnectionManager connectionManager, SqlDialect sqlDialect, OutboxCommands outboxCommands,
        Func<ISqlOutboxTransaction> outboxTransactionFactory)
    {
        this.connectionManager = connectionManager;
        this.sqlDialect = sqlDialect;
        this.outboxCommands = outboxCommands;
        this.outboxTransactionFactory = outboxTransactionFactory;
    }

    public Task<IOutboxTransaction> BeginTransaction(ContextBag context, CancellationToken cancellationToken = default)
    {
        var transaction = outboxTransactionFactory();
        transaction.Prepare(context);
        // we always need to avoid using async/await in here so that the transaction scope can float!
        return BeginTransactionInternal(transaction, context, cancellationToken);
    }

    static async Task<IOutboxTransaction> BeginTransactionInternal(ISqlOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.Begin(context, cancellationToken).ConfigureAwait(false);

            return transaction;
        }
        catch (Exception ex) when (ex.IsCausedBy(cancellationToken))
        {
            // copy the general catch but don't let another exception mask the OCE
            try
            {
                transaction.Dispose();
            }
            catch { }

            throw;
        }
        catch (Exception ex)
        {
            // A method that returns something that is disposable should not throw during the creation
            // of the disposable resource. If it does the compiler generated code will not dispose anything
            // therefore we need to dispose here to prevent the connection being returned to the pool being
            // in a zombie state.
            transaction.Dispose();
            throw new Exception("Error while opening outbox transaction", ex);
        }
    }

    public async Task SetAsDispatched(string messageId, ContextBag context, CancellationToken cancellationToken = default)
    {
        using (var connection = await connectionManager.OpenConnection(context.GetIncomingMessage(), cancellationToken).ConfigureAwait(false))
        using (var command = sqlDialect.CreateCommand(connection))
        {
            command.CommandText = outboxCommands.SetAsDispatched;
            command.AddParameter("MessageId", messageId);
            command.AddParameter("DispatchedAt", DateTime.UtcNow);
            await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<OutboxMessage> Get(string messageId, ContextBag context, CancellationToken cancellationToken = default)
    {
        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var connection = await connectionManager.OpenConnection(context.GetIncomingMessage(), cancellationToken).ConfigureAwait(false))
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            OutboxMessage result;
            using (var command = sqlDialect.CreateCommand(connection))
            {
                command.CommandText = outboxCommands.Get;
                command.Transaction = transaction;
                command.AddParameter("MessageId", messageId);

                // to avoid loading into memory SequentialAccess is required which means each fields needs to be accessed, but SequentialAccess is unsupported for SQL Server AlwaysEncrypted
                using (var dataReader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
                {
                    if (!await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return null;
                    }
                    var dispatched = await dataReader.GetBoolAsync(0, cancellationToken).ConfigureAwait(false);
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

    public Task Store(OutboxMessage message, IOutboxTransaction outboxTransaction, ContextBag context, CancellationToken cancellationToken = default) =>
        ((ISqlOutboxTransaction)outboxTransaction).Complete(message, context, cancellationToken);

    public async Task RemoveEntriesOlderThan(DateTime dateTime, CancellationToken cancellationToken = default)
    {
        const int CleanupBatchSize = 4000; // Keep below 4000 to prevent lock escalation

        using (var connection = await connectionManager.OpenNonContextualConnection(cancellationToken).ConfigureAwait(false))
        {
            var continuePurging = true;
            while (continuePurging)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var command = sqlDialect.CreateCommand(connection))
                {
                    command.CommandText = outboxCommands.Cleanup;
                    command.AddParameter("DispatchedBefore", dateTime);
                    command.AddParameter("BatchSize", CleanupBatchSize);
                    var rowCount = await command.ExecuteNonQueryEx(cancellationToken).ConfigureAwait(false);
                    continuePurging = rowCount != 0;
                }
            }
        }
    }
}