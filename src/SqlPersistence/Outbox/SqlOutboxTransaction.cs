using System.Data.Common;
using System.Threading.Tasks;
using Janitor;
using NServiceBus.Outbox;

[SkipWeaving]
class SqlOutboxTransaction : OutboxTransaction
{
    public readonly DbTransaction Transaction;
    public readonly DbConnection Connection;

    public SqlOutboxTransaction(DbTransaction transaction, DbConnection connection)
    {
        Transaction = transaction;
        Connection = connection;
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection?.Dispose();
    }

    public Task Commit()
    {
        Transaction.Commit();
        return Task.FromResult(0);
    }
}