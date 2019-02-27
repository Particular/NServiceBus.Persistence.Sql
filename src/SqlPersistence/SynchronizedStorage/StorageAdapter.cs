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
    IConnectionManager connectionBuilder;

    public StorageAdapter(IConnectionManager connectionBuilder, SagaInfoCache infoCache, SqlDialect dialect)
    {
        this.connectionBuilder = connectionBuilder;
        this.infoCache = infoCache;
        this.dialect = dialect;
    }

    public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
    {
        if (!(transaction is SqlOutboxTransaction outboxTransaction))
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