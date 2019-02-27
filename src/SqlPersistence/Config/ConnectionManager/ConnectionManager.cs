using System.Data.Common;
using NServiceBus.Transport;

abstract class ConnectionManager
{
    public abstract DbConnection BuildNonContextual();
    public abstract DbConnection Build(IncomingMessage incomingMessage);
}
