using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

class SynchronizedStorage : ISynchronizedStorage
{
    IConnectionManager connectionManager;
    SagaInfoCache infoCache;

    public SynchronizedStorage(IConnectionManager connectionManager, SagaInfoCache infoCache)
    {
        this.connectionManager = connectionManager;
        this.infoCache = infoCache;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = await connectionManager.OpenConnection(contextBag.GetIncomingMessage()).ConfigureAwait(false);
        var transaction = connection.BeginTransaction();
        var session = new StorageSession(connection, transaction, true, infoCache);
        contextBag.RegisterSession(session);
        return session;
    }
}