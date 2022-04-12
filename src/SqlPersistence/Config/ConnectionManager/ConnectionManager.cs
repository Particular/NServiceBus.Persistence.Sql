using System;
using System.Data.Common;
using NServiceBus.Transport;

class ConnectionManager : IConnectionManager
{
    Func<DbConnection> connectionBuilder;

    public ConnectionManager(Func<DbConnection> connectionBuilder) => this.connectionBuilder = connectionBuilder;

    public DbConnection BuildNonContextual() => connectionBuilder();

    public DbConnection Build(IncomingMessage incomingMessage) => connectionBuilder();
}