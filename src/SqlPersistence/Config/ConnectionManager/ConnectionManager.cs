using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Transport;

abstract class ConnectionManager
{
    public abstract Task<DbConnection> OpenNonContextualConnection();
    public abstract DbConnection BuildNonContextual();
    public abstract Task<DbConnection> OpenConnection(IncomingMessage incomingMessage);

    protected async Task<DbConnection> OpenConnection(DbConnection connection)
    {
        try
        {
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
        catch
        {
            connection?.Dispose();
            throw;
        }
    }
}
