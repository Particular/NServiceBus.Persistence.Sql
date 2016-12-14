using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

class SynchronizedStorage : ISynchronizedStorage
{
    Func<DbConnection> connectionBuilder;

    public SynchronizedStorage(Func<DbConnection> connectionBuilder)
    {
        this.connectionBuilder = connectionBuilder;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = connectionBuilder();
        await connection.OpenAsync().ConfigureAwait(false);
        var transaction = connection.BeginTransaction();
        return new StorageSession(connection, transaction,true);
    }
}