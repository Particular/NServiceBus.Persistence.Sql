using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

class StorageSession : CompletableSynchronizedStorageSession, ISqlStorageSession
{
    bool ownsTransaction;

    public StorageSession(DbConnection connection, DbTransaction transaction, bool ownsTransaction, SagaInfoCache infoCache)
    {
        Guard.AgainstNull(nameof(connection), connection);

        Connection = connection;
        this.ownsTransaction = ownsTransaction;
        InfoCache = infoCache;
        Transaction = transaction;
    }

    internal SagaInfoCache InfoCache;
    public DbTransaction Transaction { get; }
    public DbConnection Connection { get; }

    public Task CompleteAsync()
    {
        if (ownsTransaction)
        {
            Transaction?.Commit();
            Transaction?.Dispose();
            Connection.Dispose();
        }
        return Task.FromResult(0);
    }

    public void Dispose()
    {
        if (ownsTransaction)
        {
            Transaction?.Dispose();
            Connection?.Dispose();
        }
    }
}
