using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Transport;

class MultiTenantConnectionManager : ConnectionManager
{
    Func<IncomingMessage, string> captureTenantId;
    Func<string, DbConnection> buildConnectionFromTenantData;

    public MultiTenantConnectionManager(Func<IncomingMessage, string> captureTenantId, Func<string, DbConnection> buildConnectionFromTenantData)
    {
        this.captureTenantId = captureTenantId;
        this.buildConnectionFromTenantData = buildConnectionFromTenantData;
    }

    public override DbConnection BuildNonContextual()
    {
        throw new NotImplementedException();
    }

    public override Task<DbConnection> OpenConnection(IncomingMessage incomingMessage)
    {
        var tenantId = captureTenantId(incomingMessage);
        var connection = buildConnectionFromTenantData(tenantId);
        return OpenConnection(connection);
    }

    public override Task<DbConnection> OpenNonContextualConnection()
    {
        throw new NotImplementedException();
    }
}