using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

abstract class ConcurrencyControlStrategy
{
    public abstract Task Begin(string messageId, DbConnection connection, DbTransaction transaction);
    public abstract Task Complete(OutboxMessage outboxMessage, DbConnection connection, DbTransaction transaction, ContextBag context);
}