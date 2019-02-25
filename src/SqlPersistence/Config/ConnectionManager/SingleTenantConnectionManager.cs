using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

class SingleTenantConnectionManager : ConnectionManager
{
    Func<DbConnection> connectionBuilder;

    public SingleTenantConnectionManager(Func<DbConnection> connectionBuilder)
    {
        this.connectionBuilder = connectionBuilder;
    }

    public override Task<DbConnection> OpenNonContextualConnection()
    {
        return OpenConnection(connectionBuilder());
    }

    public override DbConnection BuildNonContextual()
    {
        return connectionBuilder();
    }

    public override Task<DbConnection> OpenConnection(IReadOnlyDictionary<string, string> messageHeaders)
    {
        return OpenConnection(connectionBuilder());
    }
}