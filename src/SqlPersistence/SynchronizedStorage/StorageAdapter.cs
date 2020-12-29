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
    CurrentSessionHolder currentSessionHolder;
    IConnectionManager connectionBuilder;
    bool isSequentialAccessSupported;

    public StorageAdapter(IConnectionManager connectionBuilder, SagaInfoCache infoCache, SqlDialect dialect, CurrentSessionHolder currentSessionHolder, bool isSequentialAccessSupported)
    {
        this.connectionBuilder = connectionBuilder;
        this.infoCache = infoCache;
        this.dialect = dialect;
        this.currentSessionHolder = currentSessionHolder;
        this.isSequentialAccessSupported = isSequentialAccessSupported;
    }

    public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
    {
        if (!(transaction is ISqlOutboxTransaction outboxTransaction))
        {
            return EmptyResultTask;
        }
        var session = new StorageSession(outboxTransaction.Connection, outboxTransaction.Transaction, false, infoCache, isSequentialAccessSupported);

        currentSessionHolder?.SetCurrentSession(session);

        return Task.FromResult<CompletableSynchronizedStorageSession>(session);
    }

    public async Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
    {
        var session = await dialect.TryAdaptTransportConnection(transportTransaction, context, connectionBuilder,
            (conn, trans, ownsTx) => new StorageSession(conn, trans, ownsTx, infoCache, isSequentialAccessSupported)).ConfigureAwait(false);
        if (session != null)
        {
            currentSessionHolder?.SetCurrentSession(session);
        }
        return session;
    }
}