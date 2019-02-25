using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

abstract class ConnectionManager
{
    public abstract Task<DbConnection> OpenNonContextualConnection();
    public abstract DbConnection BuildNonContextual();
    public abstract Task<DbConnection> OpenConnection(IReadOnlyDictionary<string, string> messageHeaders);

    protected async Task<DbConnection> OpenConnection(DbConnection connection)
    {
        try
        {
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}