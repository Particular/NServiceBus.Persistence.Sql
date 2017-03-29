using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Transactions;
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
        SqlConnection existingSqlConnection;
        SqlTransaction existingSqlTransaction;
        //SQL server transport in native TX mode
        if (transportTransaction.TryGet(out existingSqlConnection) && transportTransaction.TryGet(out existingSqlTransaction))
        {
            CompletableSynchronizedStorageSession session = new StorageSession(existingSqlConnection, existingSqlTransaction, false, infoCache);
            return session;
        }

        Transaction transportTx;
        // Transport supports DTC and uses TxScope owned by the transport
        transportTransaction.TryGet(out transportTx);
        if (transportTx != null && Transaction.Current != null && !transportTx.Equals(Transaction.Current))
        {
            throw new Exception("A TransctionScope has been opened in the current context overriding the one created by the transport. " 
                + "This setup can result in insonsistent data because operations done via connections enlisted in the context scope won't be committed "
                + "atomically with the receive transaction. If you wish to manually control the TransactionScope in the pipeline switch the transport transaction mode "
                + "to values lower than 'TransactionScope'.");
        }
        var ambientTransaction = transportTx ?? Transaction.Current;
        if (ambientTransaction != null)
        {
            var connection = await connectionBuilder.OpenConnection();
            connection.EnlistTransaction(ambientTransaction);
            CompletableSynchronizedStorageSession session = new StorageSession(connection, null, true, infoCache);
            return session;
        }

        //Other modes handled by creating a new session.
        return null;
    }
}