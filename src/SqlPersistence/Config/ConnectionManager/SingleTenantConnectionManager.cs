using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Transport;

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

    public override Task<DbConnection> OpenConnection(IncomingMessage incomingMessage)
    {
        return OpenConnection(connectionBuilder());
    }
}