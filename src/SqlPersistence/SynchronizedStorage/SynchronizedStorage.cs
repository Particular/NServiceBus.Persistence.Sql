using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

class SynchronizedStorage : ISynchronizedStorage
{
    Func<ContextBag, DbConnection> connectionBuilder;
    SagaInfoCache infoCache;

    public SynchronizedStorage(Func<ContextBag, DbConnection> connectionBuilder, SagaInfoCache infoCache)
    {
        this.connectionBuilder = connectionBuilder;
        this.infoCache = infoCache;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = await connectionBuilder.OpenConnection(contextBag).ConfigureAwait(false);
        var transaction = connection.BeginTransaction();
        return new StorageSession(connection, transaction, true, infoCache);
    }
}