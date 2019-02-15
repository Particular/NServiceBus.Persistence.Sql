using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

class ConnectionManager
{
    Func<IReadOnlyDictionary<string, string>, string> captureTenantId;
    Func<string, DbConnection> buildConnectionFromTenantData;

    public ConnectionManager(Func<IReadOnlyDictionary<string, string>, string> captureTenantId, Func<string, DbConnection> buildConnectionFromTenantData)
    {
        this.captureTenantId = captureTenantId;
        this.buildConnectionFromTenantData = buildConnectionFromTenantData;
    }

    public static ConnectionManager BuildSingleTenant(Func<DbConnection> connectionBuilder)
    {
        return new ConnectionManager(context => null, _ => connectionBuilder());
    }

    public DbConnection Build(IReadOnlyDictionary<string, string> messageHeaders, out string tenantId)
    {
        tenantId = captureTenantId(messageHeaders);
        return buildConnectionFromTenantData(tenantId);
    }

    public DbConnection Build(IReadOnlyDictionary<string, string> messageHeaders)
    {
        return Build(messageHeaders, out _);
    }

    public DbConnection Build(string tenantId)
    {
        return buildConnectionFromTenantData(tenantId);
    }

    public DbConnection BuildNonContextual()
    {
        return buildConnectionFromTenantData(null);
    }

    public Task<DbConnection> OpenConnection(IReadOnlyDictionary<string, string> messageHeaders)
    {
        var connection = Build(messageHeaders);
        return OpenConnection(connection);
    }

    public Task<DbConnection> OpenNonContextualConnection()
    {
        var connection = BuildNonContextual();
        return OpenConnection(connection);
    }

    public Task<DbConnection> OpenConnection(string tenantId)
    {
        var connection = Build(tenantId);
        return OpenConnection(connection);
    }

    async Task<DbConnection> OpenConnection(DbConnection connection)
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