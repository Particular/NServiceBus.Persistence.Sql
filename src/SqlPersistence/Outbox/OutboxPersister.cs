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
#pragma warning disable 618

class OutboxPersister : IOutboxStorage
{
    Func<DbConnection> connectionBuilder;
    int cleanupBatchSize;
    OutboxCommands outboxCommands;

    public OutboxPersister(Func<DbConnection> connectionBuilder, string tablePrefix, string schema, SqlVariant sqlVariant, int cleanupBatchSize = 10000)
    {
        this.connectionBuilder = connectionBuilder;
        this.cleanupBatchSize = cleanupBatchSize;
        outboxCommands = OutboxCommandBuilder.Build(tablePrefix, schema, sqlVariant);
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
                    if (await dataReader.IsDBNullAsync(0))
                    {
                        result = new OutboxMessage(messageId, new TransportOperation[0]);
                    }
                    else
                    {
                        using (var textReader = dataReader.GetTextReader(0))
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
        var sqlOutboxTransaction = (SqlOutboxTransaction)outboxTransaction;
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
}