using System.Data.SqlClient;
using System.Threading.Tasks;
using Janitor;
using NServiceBus.Persistence;
using NServiceBus.Persistence.Sql;

[SkipWeaving]
class StorageSession : CompletableSynchronizedStorageSession, ISqlStorageSession
{
    bool ownsTransaction;

    public StorageSession(SqlConnection connection, SqlTransaction transaction, bool ownsTransaction)
    {
        Guard.AgainstNull(nameof(connection), connection);
        Guard.AgainstNull(nameof(transaction), transaction);
        Connection = connection;
        this.ownsTransaction = ownsTransaction;
        Transaction = transaction;
    }

    public SqlTransaction Transaction { get; }
    public SqlConnection Connection { get; }

    public Task CompleteAsync()
    {
        if (ownsTransaction)
        {
            if (Transaction != null)
            {
                Transaction.Commit();
                Transaction.Dispose();
            }
            Connection.Dispose();
        }
        return Task.FromResult(0);
    }

    public void Dispose()
    {
        if (ownsTransaction)
        {
            Transaction?.Dispose();
            Connection.Dispose();
        }
    }
}
