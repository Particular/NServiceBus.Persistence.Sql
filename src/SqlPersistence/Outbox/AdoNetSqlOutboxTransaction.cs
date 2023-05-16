using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Outbox;

class AdoNetSqlOutboxTransaction : ISqlOutboxTransaction
{
    IConnectionManager connectionManager;
    IsolationLevel isolationLevel;
    ConcurrencyControlStrategy concurrencyControlStrategy;

    public AdoNetSqlOutboxTransaction(ConcurrencyControlStrategy concurrencyControlStrategy,
        IConnectionManager connectionManager, IsolationLevel isolationLevel)
    {
        this.connectionManager = connectionManager;
        this.isolationLevel = isolationLevel;
        this.concurrencyControlStrategy = concurrencyControlStrategy;
    }

    public DbTransaction Transaction { get; private set; }
    public DbConnection Connection { get; private set; }

    public void Prepare(ContextBag context)
    {
        //NOOP
    }

    public async Task Begin(ContextBag context, CancellationToken cancellationToken = default)
    {
        var incomingMessage = context.GetIncomingMessage();
        Connection = await connectionManager.OpenConnection(incomingMessage, cancellationToken).ConfigureAwait(false);
        Transaction = Connection.BeginTransaction(isolationLevel);
        await concurrencyControlStrategy.Begin(incomingMessage.MessageId, Connection, Transaction, cancellationToken).ConfigureAwait(false);
    }

    public Task Complete(OutboxMessage outboxMessage, ContextBag context, CancellationToken cancellationToken = default) =>
        concurrencyControlStrategy.Complete(outboxMessage, Connection, Transaction, context, cancellationToken);

    public void Dispose()
    {
        Transaction?.Dispose();
        Connection?.Dispose();
    }

    public Task Commit(CancellationToken cancellationToken = default)
    {
        Transaction.Commit();
        return Task.CompletedTask;
    }
}