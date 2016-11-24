using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence;

class SynchronizedStorage : ISynchronizedStorage
{
    string connectionString;

    public SynchronizedStorage(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
    {
        var connection = await SqlHelpers.New(connectionString);
        var transaction = connection.BeginTransaction();
        return new StorageSession(connection, transaction,true);
    }
}