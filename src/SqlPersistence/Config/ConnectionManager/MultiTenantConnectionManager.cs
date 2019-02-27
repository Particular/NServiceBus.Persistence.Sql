using System;
using System.Data.Common;
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

    public override DbConnection Build(IncomingMessage incomingMessage)
    {
        var tenantId = captureTenantId(incomingMessage);
        return buildConnectionFromTenantData(tenantId);
    }
}