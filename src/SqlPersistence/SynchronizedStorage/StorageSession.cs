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

class StorageSession : ICompletableSynchronizedStorageSession, ISqlStorageSession
{
    bool ownsTransaction;
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
        Guard.AgainstNull(nameof(callback), callback);

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
            await Transaction.DisposeAsync().ConfigureAwait(false);

            Connection.Dispose();
        }
    }

    public void Dispose()
    {
        if (ownsTransaction)
        {
            Transaction?.Dispose();
            Connection?.Dispose();
        }
    }
}
