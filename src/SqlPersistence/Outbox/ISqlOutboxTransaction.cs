using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

interface ISqlOutboxTransaction : OutboxTransaction
{
    DbTransaction Transaction { get; }
    DbConnection Connection { get; }

    void Prepare(ContextBag context);
    Task<OutboxTransaction> Begin(ContextBag context);
    Task Complete(OutboxMessage outboxMessage, ContextBag context);
    void BeginSynchronizedSession(ContextBag context);
}