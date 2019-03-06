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

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new Exception(@"This endpoint attempted to process a message in multi-tenant mode and was unable to determine the tenant id from the incoming message. As a result SQL Persistence cannot determine which tenant database to use. Either: 1) The message lacks a tenant id and is invalid. 2) The lambda provided to determine the tenant id from an incoming message contains a bug. 3) Either this endpoint or another upstream endpoint is not configured to use a custom behavior for relaying tenant information from incoming to outgoing messages, or that behavior contains a bug.");
        }

        return buildConnectionFromTenantData(tenantId);
    }
}