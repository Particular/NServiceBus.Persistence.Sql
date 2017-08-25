using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transport;

class StorageAdapter : ISynchronizedStorageAdapter
{
    static Task<CompletableSynchronizedStorageSession> EmptyResultTask = Task.FromResult(default(CompletableSynchronizedStorageSession));

    SagaInfoCache infoCache;
    SqlDialect dialect;
    Func<DbConnection> connectionBuilder;

    public StorageAdapter(Func<DbConnection> connectionBuilder, SagaInfoCache infoCache, SqlDialect dialect)
    {
        this.connectionBuilder = connectionBuilder;
        this.infoCache = infoCache;
        this.dialect = dialect;
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

    public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
    {
        return dialect.TryAdaptTransportConnection(transportTransaction, context, connectionBuilder, 
            (conn, trans, ownsTx) => new StorageSession(conn, trans, ownsTx, infoCache));
    }
}