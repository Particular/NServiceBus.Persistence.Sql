using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

class SynchronizedStorage : ISynchronizedStorage
{
    IConnectionManager connectionManager;
    SagaInfoCache infoCache;
    CurrentSessionHolder currentSessionHolder;

    public SynchronizedStorage(IConnectionManager connectionManager, SagaInfoCache infoCache, CurrentSessionHolder currentSessionHolder)
    {
        this.connectionManager = connectionManager;
        this.infoCache = infoCache;
        this.currentSessionHolder = currentSessionHolder;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = await connectionManager.OpenConnection(contextBag.GetIncomingMessage()).ConfigureAwait(false);
        var transaction = connection.BeginTransaction();
        var session = new StorageSession(connection, transaction, true, infoCache);

        currentSessionHolder?.SetCurrentSession(session);
        return session;
    }
}