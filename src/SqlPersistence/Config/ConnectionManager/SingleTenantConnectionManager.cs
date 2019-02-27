using System;
using System.Data.Common;
using NServiceBus.Transport;

class SingleTenantConnectionManager : ConnectionManager
{
    Func<DbConnection> connectionBuilder;

    public SingleTenantConnectionManager(Func<DbConnection> connectionBuilder)
    {
        this.connectionBuilder = connectionBuilder;
    }

    public override DbConnection BuildNonContextual()
    {
        return connectionBuilder();
    }

    public override DbConnection Build(IncomingMessage incomingMessage)
    {
        return connectionBuilder();
    }
}