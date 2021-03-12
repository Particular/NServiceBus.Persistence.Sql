using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

class StorageSession : CompletableSynchronizedStorageSession, ISqlStorageSession
{
    bool ownsTransaction;
    Func<ISqlStorageSession, Task> onSaveChangesCallback = s => Task.CompletedTask;
    Action disposedCallback = () => { };

    public StorageSession(DbConnection connection, DbTransaction transaction, bool ownsTransaction, SagaInfoCache infoCache)
    {
        Guard.AgainstNull(nameof(connection), connection);

        Connection = connection;
        this.ownsTransaction = ownsTransaction;
        InfoCache = infoCache;
        Transaction = transaction;
    }

    internal SagaInfoCache InfoCache { get; }
    public DbTransaction Transaction { get; }
    public DbConnection Connection { get; }
    public void OnSaveChanges(Func<ISqlStorageSession, Task> callback)
    {
        Guard.AgainstNull(nameof(callback), callback);
        var oldCallback = onSaveChangesCallback;
        onSaveChangesCallback = async s =>
        {
            await oldCallback(s).ConfigureAwait(false);
            await callback(s).ConfigureAwait(false);
        };
    }

    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        await onSaveChangesCallback(this).ConfigureAwait(false);
        if (ownsTransaction)
        {
            Transaction?.Commit();
            Transaction?.Dispose();
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

        disposedCallback();
    }

    public void OnDisposed(Action callback)
    {
        disposedCallback = callback;
    }
}
