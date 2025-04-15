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

sealed class StorageSession
    : ICompletableSynchronizedStorageSession
    , ISqlStorageSession
#if NET
    , IAsyncDisposable
#endif
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
        Guard.AgainstNull(nameof(callback), callback);

        var oldCallback = onSaveChangesCallback;

        onSaveChangesCallback = async (session, cancellationToken) =>
        {
            await oldCallback(session, cancellationToken).ConfigureAwait(false);
            await callback(session, cancellationToken).ConfigureAwait(false);
        };
    }

    [ObsoleteEx(Message = "Use the overload that supports cancellation.", TreatAsErrorFromVersion = "7", RemoveInVersion = "8")]
#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
    public void OnSaveChanges(Func<ISqlStorageSession, Task> callback) => throw new NotImplementedException();
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext

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
#if NET
        Transaction = await Connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
#else
        Transaction = Connection.BeginTransaction();
#endif
        ownsTransaction = true;
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await onSaveChangesCallback(this, cancellationToken).ConfigureAwait(false);

        if (ownsTransaction && Transaction != null)
        {
#if NET
            await Transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await DisposeAsync().ConfigureAwait(false);
#else
            Transaction.Commit();
            Dispose();
#endif
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (ownsTransaction)
        {
            Transaction?.Dispose();
            Transaction = null;
            Connection?.Dispose();
            Connection = null;
        }

        disposed = true;
    }
#if NET
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        if (ownsTransaction)
        {
            if (Transaction != null)
            {
                await Transaction.DisposeAsync().ConfigureAwait(false);
                Transaction = null;
            }

            if (Connection != null)
            {
                await Connection.DisposeAsync().ConfigureAwait(false);
                Connection = null;
            }
        }

        disposed = true;
    }
#endif
}