using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

interface ISqlOutboxTransaction : IOutboxTransaction
{
    DbTransaction Transaction { get; }
    DbConnection Connection { get; }

    // Prepare is deliberately kept sync to allow floating of TxScope where needed
    void Prepare(ContextBag context);
    Task Begin(ContextBag context, CancellationToken cancellationToken = default);
    Task Complete(OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default);
    void BeginSynchronizedSession(ContextBag context);
}