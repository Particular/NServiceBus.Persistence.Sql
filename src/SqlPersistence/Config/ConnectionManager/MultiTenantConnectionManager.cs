using System;
using System.Data.Common;
using NServiceBus.Transport;

class MultiTenantConnectionManager : IConnectionManager
{
    Func<IncomingMessage, string> captureTenantId;
    Func<string, DbConnection> buildConnectionFromTenantData;

    public MultiTenantConnectionManager(Func<IncomingMessage, string> captureTenantId, Func<string, DbConnection> buildConnectionFromTenantData)
    {
        this.captureTenantId = captureTenantId;
        this.buildConnectionFromTenantData = buildConnectionFromTenantData;
    }

    public DbConnection BuildNonContextual()
    {
        throw new NotImplementedException();
    }

    public DbConnection Build(IncomingMessage incomingMessage)
    {
        var tenantId = captureTenantId(incomingMessage);
        return buildConnectionFromTenantData(tenantId);
    }
}