using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

class SynchronizedStorage : ISynchronizedStorage
{
    IConnectionManager connectionManager;
    SagaInfoCache infoCache;
    CurrentSessionHolder currentSessionHolder;
    bool isSequentialAccessSupported;

    public SynchronizedStorage(IConnectionManager connectionManager, SagaInfoCache infoCache, CurrentSessionHolder currentSessionHolder, bool isSequentialAccessSupported)
    {
        this.connectionManager = connectionManager;
        this.infoCache = infoCache;
        this.currentSessionHolder = currentSessionHolder;
        this.isSequentialAccessSupported = isSequentialAccessSupported;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = await connectionManager.OpenConnection(contextBag.GetIncomingMessage()).ConfigureAwait(false);
        var transaction = connection.BeginTransaction();
        var session = new StorageSession(connection, transaction, true, infoCache, isSequentialAccessSupported);

        currentSessionHolder?.SetCurrentSession(session);
        return session;
    }
}