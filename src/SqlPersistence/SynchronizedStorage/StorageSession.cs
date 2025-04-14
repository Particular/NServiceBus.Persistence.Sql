using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;
using NServiceBus.Transport;

class StorageSession : ICompletableSynchronizedStorageSession, ISqlStorageSession, IAsyncDisposable
{
    bool ownsTransaction;
    bool disposed;
    Func<ISqlStorageSession, CancellationToken, Task> onSaveChangesCallback = (_, __) => Task.CompletedTask;
    readonly IConnectionManager connectionManager;
    readonly SqlDialect dialect;

    public StorageSession(IConnectionManager connectionManager, SagaInfoCache infoCache, SqlDialect dialect)
    {
        this.dialect = dialect;
        this.connectionManager = connectionManager;
        InfoCache = infoCache;
    }

    internal SagaInfoCache InfoCache { get; }
    public DbTransaction Transaction { get; private set; }
    public DbConnection Connection { get; private set; }

    public void OnSaveChanges(Func<ISqlStorageSession, CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var oldCallback = onSaveChangesCallback;

        onSaveChangesCallback = async (session, cancellationToken) =>
        {
            await oldCallback(session, cancellationToken).ConfigureAwait(false);
            await callback(session, cancellationToken).ConfigureAwait(false);
        };
    }

    public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        if (transaction is not ISqlOutboxTransaction outboxTransaction)
        {
            return new ValueTask<bool>(false);
        }

        Connection = outboxTransaction.Connection;
        ownsTransaction = false;
        Transaction = outboxTransaction.Transaction;
        return new ValueTask<bool>(true);
    }

    public async ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
        CancellationToken cancellationToken = default)
    {
        (bool wasAdapted, DbConnection connection, DbTransaction transaction, bool ownsTx) =
            await dialect.TryAdaptTransportConnection(transportTransaction, context, connectionManager, cancellationToken)
            .ConfigureAwait(false);
        if (!wasAdapted)
        {
            return false;
        }

        Connection = connection;
        Transaction = transaction;
        ownsTransaction = ownsTx;
        return true;
    }

    public async Task Open(ContextBag contextBag, CancellationToken cancellationToken = default)
    {
        Connection = await connectionManager.OpenConnection(contextBag.GetIncomingMessage(), cancellationToken).ConfigureAwait(false);
        Transaction = await Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        ownsTransaction = true;
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await onSaveChangesCallback(this, cancellationToken).ConfigureAwait(false);

        if (ownsTransaction && Transaction != null)
        {
            await Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            if (ownsTransaction)
            {
                Transaction?.Dispose();
                Transaction = null;

                Connection?.Dispose();
                Connection = null;
            }
        }

        disposed = false;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }

#pragma warning disable PS0018
    protected virtual async ValueTask DisposeAsyncCore()
#pragma warning restore PS0018
    {
        if (ownsTransaction)
        {
            if (Transaction != null)
            {
                //DbTransaction is required to implement IAsyncDisposable
                await Transaction.DisposeAsync().ConfigureAwait(false);
                Transaction = null;
            }

            if (Connection != null)
            {
                //DbConnect is required to implement IAsyncDisposable
                await Connection.DisposeAsync().ConfigureAwait(false);
                Connection = null;
            }
        }
    }
}
