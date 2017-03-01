using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

class SynchronizedStorage : ISynchronizedStorage
{
    Func<DbConnection> connectionBuilder;
    SagaInfoCache infoCache;

    public SynchronizedStorage(Func<DbConnection> connectionBuilder, SagaInfoCache infoCache)
    {
        this.connectionBuilder = connectionBuilder;
        this.infoCache = infoCache;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = await connectionBuilder.OpenConnection();
        var transaction = connection.BeginTransaction();
        Guard.AgainstNull(nameof(connection), connection);
        Guard.AgainstNull(nameof(transaction), transaction);
        return new StorageSession(connection, transaction, true, infoCache);
    }
}