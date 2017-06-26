using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transport;

class StorageAdapter : ISynchronizedStorageAdapter
{
    static Task<CompletableSynchronizedStorageSession> EmptyResultTask = Task.FromResult(default(CompletableSynchronizedStorageSession));

    SagaInfoCache infoCache;
    Func<DbConnection> connectionBuilder;

    public StorageAdapter(Func<DbConnection> connectionBuilder, SagaInfoCache infoCache)
    {
        this.connectionBuilder = connectionBuilder;
        this.infoCache = infoCache;
    }

    public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
    {
        var outboxTransaction = transaction as SqlOutboxTransaction;
        if (outboxTransaction == null)
        {
            return EmptyResultTask;
        }
        CompletableSynchronizedStorageSession session = new StorageSession(outboxTransaction.Connection, outboxTransaction.Transaction, false, infoCache);
        return Task.FromResult(session);
    }

    public async Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
    {
        //SQL server transport in native TX mode
        if (transportTransaction.TryGet(out SqlConnection existingSqlConnection) &&
            transportTransaction.TryGet(out SqlTransaction existingSqlTransaction))
        {
            return new StorageSession(existingSqlConnection, existingSqlTransaction, false, infoCache);
        }

        // Transport supports DTC and uses TxScope owned by the transport
        var scopeTx = Transaction.Current;
        if (transportTransaction.TryGet(out Transaction transportTx) &&
            scopeTx != null &&
            transportTx != scopeTx)
        {
            throw new Exception("A TransactionScope has been opened in the current context overriding the one created by the transport. "
                + "This setup can result in inconsistent data because operations done via connections enlisted in the context scope won't be committed "
                + "atomically with the receive transaction. To manually control the TransactionScope in the pipeline switch the transport transaction mode "
                + $"to values lower than '{nameof(TransportTransactionMode.TransactionScope)}'.");
        }
        var ambientTransaction = transportTx ?? scopeTx;
        if (ambientTransaction != null)
        {
            var connection = await connectionBuilder.OpenConnection().ConfigureAwait(false);
            connection.EnlistTransaction(ambientTransaction);
            return new StorageSession(connection, null, true, infoCache);
        }

        //Other modes handled by creating a new session.
        return null;
    }
}