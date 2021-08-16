using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

class StorageSession : ICompletableSynchronizedStorageSession, ISqlStorageSession
{
    bool ownsTransaction;
    Func<ISqlStorageSession, CancellationToken, Task> onSaveChangesCallback = (_, __) => Task.CompletedTask;
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

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await onSaveChangesCallback(this, cancellationToken).ConfigureAwait(false);

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
