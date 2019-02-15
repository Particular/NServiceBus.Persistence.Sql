using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

class SynchronizedStorage : ISynchronizedStorage
{
    ConnectionManager connectionManager;
    SagaInfoCache infoCache;

    public SynchronizedStorage(ConnectionManager connectionManager, SagaInfoCache infoCache)
    {
        this.connectionManager = connectionManager;
        this.infoCache = infoCache;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = await connectionManager.OpenConnection(contextBag.GetIncomingContext()).ConfigureAwait(false);
        var transaction = connection.BeginTransaction();
        return new StorageSession(connection, transaction, true, infoCache);
    }
}