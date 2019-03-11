using System.Data.Common;
using NServiceBus.Transport;

interface IConnectionManager
{
    DbConnection BuildNonContextual();
    DbConnection Build(IncomingMessage incomingMessage);
}
