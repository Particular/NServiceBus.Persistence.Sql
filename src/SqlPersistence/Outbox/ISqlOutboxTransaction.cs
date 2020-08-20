using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

interface ISqlOutboxTransaction : OutboxTransaction
{
    DbTransaction Transaction { get; }
    DbConnection Connection { get; }

    // Prepare is deliberately kept sync to allow floating of TxScope where needed
    void Prepare(ContextBag context);
    Task Begin(ContextBag context);
    Task Complete(OutboxMessage outboxMessage, ContextBag context);
    void BeginSynchronizedSession(ContextBag context);
}