using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus.Outbox;

class SqlOutboxTransaction : OutboxTransaction
{
    public readonly SqlTransaction SqlTransaction;

    public SqlOutboxTransaction(SqlTransaction sqlTransaction)
    {
        SqlTransaction = sqlTransaction;
    }

    public void Dispose()
    {
        SqlTransaction?.Dispose();
    }

    public Task Commit()
    {
        SqlTransaction.Commit();
        return Task.FromResult(0);
    }
}