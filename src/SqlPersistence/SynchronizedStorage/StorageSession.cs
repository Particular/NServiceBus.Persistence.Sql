using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

class StorageSession : CompletableSynchronizedStorageSession, ISqlStorageSession
{
    bool ownsTransaction;
    Func<ISqlStorageSession, Task> onSaveChangesCallback;
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
        if (onSaveChangesCallback != null)
        {
            throw new Exception("Save changes callback for this session has already been registered.");
        }
        onSaveChangesCallback = callback;
    }

    public async Task CompleteAsync()
    {
        if (onSaveChangesCallback != null)
        {
            await onSaveChangesCallback(this).ConfigureAwait(false);
        }
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
