using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

class MultiTenantConnectionManager : ConnectionManager
{
    Func<IReadOnlyDictionary<string, string>, string> captureTenantId;
    Func<string, DbConnection> buildConnectionFromTenantData;

    public MultiTenantConnectionManager(Func<IReadOnlyDictionary<string, string>, string> captureTenantId, Func<string, DbConnection> buildConnectionFromTenantData)
    {
        this.captureTenantId = captureTenantId;
        this.buildConnectionFromTenantData = buildConnectionFromTenantData;
    }

    public override DbConnection BuildNonContextual()
    {
        throw new NotImplementedException();
    }

    public override Task<DbConnection> OpenConnection(IReadOnlyDictionary<string, string> messageHeaders)
    {
        var tenantId = captureTenantId(messageHeaders);
        var connection = buildConnectionFromTenantData(tenantId);
        return OpenConnection(connection);
    }

    public override Task<DbConnection> OpenNonContextualConnection()
    {
        throw new NotImplementedException();
    }
}