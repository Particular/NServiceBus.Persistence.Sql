using System.Threading.Tasks;
using NServiceBus;
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
        var messageHandlerContext = contextBag.Get<IMessageHandlerContext>();
        var connection = await connectionManager.OpenConnection(messageHandlerContext).ConfigureAwait(false);
        var transaction = connection.BeginTransaction();
        return new StorageSession(connection, transaction, true, infoCache);
    }
}