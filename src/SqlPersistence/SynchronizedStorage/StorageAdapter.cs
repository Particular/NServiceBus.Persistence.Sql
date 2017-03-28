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

        Transaction existingTransaction;
        // Transport supports DTC and uses TxScope owned by the transport
        if (transportTransaction.TryGet(out existingTransaction))
        {
            var connection = await connectionBuilder.OpenConnection();
            CompletableSynchronizedStorageSession session = new StorageSession(connection, null, true, infoCache);
            return session;
        }

        //Other modes handled by creating a new session.
        return null;
    }
}