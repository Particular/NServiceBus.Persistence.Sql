using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Transport;

class StorageAdapter : ISynchronizedStorageAdapter
{
    static Task<ICompletableSynchronizedStorageSession> EmptyResultTask = Task.FromResult(default(ICompletableSynchronizedStorageSession));

    SagaInfoCache infoCache;
    SqlDialect dialect;
    CurrentSessionHolder currentSessionHolder;
    IConnectionManager connectionBuilder;

    public StorageAdapter(IConnectionManager connectionBuilder, SagaInfoCache infoCache, SqlDialect dialect, CurrentSessionHolder currentSessionHolder)
    {
        this.connectionBuilder = connectionBuilder;
        this.infoCache = infoCache;
        this.dialect = dialect;
        this.currentSessionHolder = currentSessionHolder;
    }

    public Task<ICompletableSynchronizedStorageSession> TryAdapt(IOutboxTransaction transaction, ContextBag context, CancellationToken cancellationToken = default)
    {
        if (transaction is not ISqlOutboxTransaction outboxTransaction)
        {
            return EmptyResultTask;
        }
        var session = new StorageSession(outboxTransaction.Connection, outboxTransaction.Transaction, false, infoCache);

        currentSessionHolder?.SetCurrentSession(session);

        return Task.FromResult<ICompletableSynchronizedStorageSession>(session);
    }

    public async Task<ICompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context, CancellationToken cancellationToken = default)
    {
        var session = await dialect.TryAdaptTransportConnection(
            transportTransaction,
            context,
            connectionBuilder,
            (conn, trans, ownsTx) => new StorageSession(conn, trans, ownsTx, infoCache),
            cancellationToken).ConfigureAwait(false);

        if (session != null)
        {
            currentSessionHolder?.SetCurrentSession(session);
        }

        return session;
    }
}