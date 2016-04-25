using System.Data.SqlClient;
using System.Threading.Tasks;
using Janitor;
using NServiceBus.Outbox;

[SkipWeaving]
class SqlOutboxTransaction : OutboxTransaction
{
    public readonly SqlTransaction SqlTransaction;
    public readonly SqlConnection SqlConnection;

    public SqlOutboxTransaction(SqlTransaction sqlTransaction, SqlConnection sqlConnection)
    {
        SqlTransaction = sqlTransaction;
        SqlConnection = sqlConnection;
    }

    public void Dispose()
    {
        SqlTransaction?.Dispose();
        SqlConnection?.Dispose();
    }

    public Task Commit()
    {
        SqlTransaction.Commit();
        return Task.FromResult(0);
    }
}